"""Run against the local Development app. Creates a uniquely named test student/batch."""
import http.cookiejar
import re
import urllib.parse
import urllib.request
import uuid
import os

BASE = os.environ.get("EXAM_TEST_URL", "http://localhost:5080")
ADMIN_EMAIL = os.environ.get("EXAM_ADMIN_EMAIL", "admin@example.test")
ADMIN_PASSWORD = os.environ.get("EXAM_ADMIN_PASSWORD", "replace-with-local-admin-password")

def browser():
    return urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

def get(client, path):
    return client.open(BASE + path).read().decode()

def post(client, path, source, fields):
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', source).group(1)
    fields["__RequestVerificationToken"] = token
    return client.open(BASE + path, urllib.parse.urlencode(fields).encode()).read().decode()

admin = browser()
page = post(admin, "/Account/Login", get(admin, "/Account/Login"), {"Email": ADMIN_EMAIL, "Password": ADMIN_PASSWORD})
assert "TEACHING WORKSPACE" in page
name = "Smoke " + uuid.uuid4().hex[:8]
page = post(admin, "/Portal/CreateBatch", page, {"name": name, "description": "Razor smoke test"})
assert name in page
batch_id = re.search(r'/Portal/CreateExam/([a-f0-9-]+)', page).group(1)
assert "permissions" in get(admin, "/Portal/Users")
student = browser()
page = post(student, "/Account/Register", get(student, "/Account/Register"), {"Name": name, "Email": uuid.uuid4().hex + "@example.test", "Password": "SmokeTest123!"})
assert "STUDENT WORKSPACE" in page
page = post(student, "/Portal/Enroll/" + batch_id, page, {})
assert "You are enrolled" in page
page = post(admin, "/Portal/CreateExam", get(admin, "/Portal/CreateExam/" + batch_id), {"BatchId": batch_id, "Title": name + " exam", "FullMarks": "100", "Release": "2027-01-01T09:00", "End": "2027-01-01T10:00", "Deadline": "2027-01-01T10:15"})
assert "Question paper" in page
assert "Review submissions" in page
assert "Exam leaderboard" in page
assert "Sign in" in get(browser(), "/Portal/Index")
print("PASS: Razor login, admin dashboard/users, batch creation, student registration/enrollment, scheduling, exam view, anonymous redirect")
exam_id = re.search(r'/Portal/EditExam/([a-f0-9-]+)', page).group(1)
page = post(admin, "/Portal/EditBatch/" + batch_id, get(admin, "/Portal/EditBatch/" + batch_id), {"Name": name + " updated", "Description": "Edited", "Status": "Open", "EnrollmentDeadline": "2028-01-01T12:00"})
assert name + " updated" in page
page = post(admin, "/Portal/EditExam/" + exam_id, get(admin, "/Portal/EditExam/" + exam_id), {"Title": "Edited exam", "FullMarks": "150", "Release": "2020-01-01T09:00", "End": "2030-01-01T10:00", "Deadline": "2030-01-01T10:15"})
assert "Edited exam" in page and "Exam updated" in page, page
assert "Access unavailable" in get(student, "/Portal/EditExam/" + exam_id)
# Upload a question after the exam has started, using the real antiforgery form.
token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
boundary = uuid.uuid4().hex
body = (f'--{boundary}\r\nContent-Disposition: form-data; name="__RequestVerificationToken"\r\n\r\n{token}\r\n'
        f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="question.pdf"\r\nContent-Type: application/pdf\r\n\r\n%PDF-1.4\n%%EOF\r\n--{boundary}--\r\n').encode()
request = urllib.request.Request(BASE + "/Portal/Question/" + exam_id, body, {"Content-Type": "multipart/form-data; boundary=" + boundary})
page = admin.open(request).read().decode()
assert "Question PDF uploaded" in page
assert student.open(BASE + "/Portal/DownloadQuestion/" + exam_id).read().startswith(b"%PDF-")
page = post(admin, "/Portal/DeleteExam/" + exam_id, get(admin, "/Portal/DeleteExam/" + exam_id), {})
assert "Exam and its submissions" in page
page = post(admin, "/Portal/DeleteBatch/" + batch_id, get(admin, "/Portal/DeleteBatch/" + batch_id), {})
assert "Batch and all associated" in page
print("PASS: edit batch/exam, student access denied, question upload after start/download, confirmed exam/batch deletion")

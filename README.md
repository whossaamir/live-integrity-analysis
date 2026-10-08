# Academic Integrity UI + Live Scan Upgrade

This version adds a colorful student dashboard and a Phase 2 Live Scan prototype.

## Live Scan features
- Webcam preview using the browser camera.
- Microphone capture.
- Live speech-to-text using the browser Web Speech API (Chrome/Edge desktop recommended).
- Automatic wording/similarity analysis every 5 seconds through ASP.NET -> Python.
- Live word count, similarity, overall risk, and status badge.

## Important
- Camera video is displayed locally in the browser and is not uploaded by this prototype.
- The spoken transcript is sent to the local ASP.NET/Python services for analysis while Live Scan is running.
- This is an integrity risk signal, not a definitive cheating or AI-use detector.
- Use the same startup order: Python :8000, ASP.NET :5000, Node.js :3000.

## Start

### Python (first time only)
```cmd
cd C:\AcademicIntegrity_Simple_Project\python-engine
pip install -r requirements.txt
python app.py
```

### ASP.NET
```cmd
cd C:\AcademicIntegrity_Simple_Project\aspnet-backend
dotnet run
```

### Node.js
```cmd
cd C:\AcademicIntegrity_UI_Upgrade\node-student-portal
npm install
npm start
```

Open http://localhost:3000 and choose **Live Scan**.


## Professional UI / Live Gaze / 48-Hour History
- Purple/blue professional student portal UI.
- Live camera and microphone.
- MediaPipe Face Mesh iris-based gaze direction signal (CENTER/LEFT/RIGHT/UP/DOWN/NO FACE).
- Gaze away percentage and glance count for examiner review.
- Submission history persists locally and retains only the most recent 48 hours.
- Gaze is an attention signal only, not proof of misconduct.

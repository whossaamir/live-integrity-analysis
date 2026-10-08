from flask import Flask, request, jsonify
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
import re

app = Flask(__name__)

REFERENCE_TEXTS = [
    "Input validation is important in secure software because untrusted input can cause unexpected behavior.",
    "Software security requires developers to validate user input before processing it.",
    "Authentication protects systems by checking the identity of a user.",
    "A secure password should be long, unique, and difficult to guess.",
    "Access control ensures that users can only access resources they are authorized to use."
]

def similarity(text):
    docs = REFERENCE_TEXTS + [text]
    matrix = TfidfVectorizer(stop_words="english").fit_transform(docs)
    return float(max(cosine_similarity(matrix[-1], matrix[:-1])[0]))

def ai_risk(text):
    words = re.findall(r"\b\w+\b", text.lower())
    if not words:
        return 0.0
    unique_ratio = len(set(words)) / len(words)
    markers = ["furthermore", "moreover", "consequently", "therefore", "in conclusion"]
    marker_count = sum(text.lower().count(x) for x in markers)
    risk = 0.20
    if len(words) > 80: risk += 0.20
    if unique_ratio > 0.65: risk += 0.15
    if marker_count >= 2: risk += 0.20
    return min(1.0, risk)

@app.get("/")
def home():
    return jsonify({"service": "Python Analysis Engine", "status": "running"})

@app.post("/analyze")
def analyze():
    data = request.get_json(silent=True) or {}
    text = str(data.get("content", "")).strip()
    if not text:
        return jsonify({"error": "No content supplied"}), 400
    sim = similarity(text)
    ai = ai_risk(text)
    overall = min(1.0, sim * 0.70 + ai * 0.30)
    if overall >= 0.70:
        message = "High risk: examiner review recommended."
    elif overall >= 0.40:
        message = "Medium risk: possible similarity detected."
    else:
        message = "Low risk: no strong similarity detected."
    return jsonify({
        "similarity": round(sim, 4),
        "aiRisk": round(ai, 4),
        "overallRisk": round(overall, 4),
        "message": message
    })

if __name__ == "__main__":
    print("Python AI engine running at http://localhost:8000")
    app.run(host="0.0.0.0", port=8000, debug=False)

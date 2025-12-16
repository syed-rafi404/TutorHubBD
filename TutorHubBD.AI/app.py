from flask import Flask, jsonify, request
from flask_cors import CORS

app = Flask(__name__)
CORS(app)  # Enable Cross-Origin Resource Sharing so ASP.NET can call this

# SRS 3.1.4 AI-Powered Recommendations
# Endpoint to check if the AI service is running
@app.route('/status', methods=['GET'])
def status():
    return jsonify({
        "status": "online",
        "service": "TutorHubBD AI Engine",
        "version": "1.0"
    })

# SRS FR-16 & FR-17: Recommendation Endpoint
# We will implement the actual NLP logic here in Sprint 2
@app.route('/recommend', methods=['POST'])
def recommend_teachers():
    # Placeholder for Sprint 1
    data = request.json
    guardian_prompt = data.get('prompt', '')
    
    # TODO (Sprint 2): Implement NLP extraction logic here
    # 1. Parse prompt (e.g., "Need math teacher in Mirpur")
    # 2. Filter tutors from database (or cached list)
    # 3. Return ranked list
    
    return jsonify({
        "message": "AI Recommendation Endpoint Reachable",
        "prompt_received": guardian_prompt,
        "recommended_tutors": [] 
    })

if __name__ == '__main__':
    # Run on port 5000 (Standard Flask Port)
    app.run(debug=True, port=5000)
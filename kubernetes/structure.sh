#!/bin/bash

# File Structure Visualization Script for Kubernetes Implementation
# This script displays the complete Kubernetes directory structure

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo ""
echo "╔════════════════════════════════════════════════════════════════════════════════╗"
echo "║                THE THRONE OF GAMES - KUBERNETES IMPLEMENTATION                 ║"
echo "║                       Phase 4.2 - Complete Structure                           ║"
echo "╚════════════════════════════════════════════════════════════════════════════════╝"
echo ""

echo "📦 KUBERNETES DIRECTORY STRUCTURE"
echo ""

tree -L 3 -I '__pycache__|node_modules|.git' --dirsfirst "$SCRIPT_DIR" || find "$SCRIPT_DIR" -type f -name "*.yaml" -o -name "*.sh" -o -name "*.md" | sort | sed 's|'"$SCRIPT_DIR"'||g' | awk '
BEGIN {
    print "kubernetes/"
}
{
    depth = gsub(/\//, "/") - 1
    path = $0
    gsub(/.*\//, "", path)
    
    for (i = 0; i < depth; i++) prefix = prefix "  "
    
    if (path ~ /\.yaml$/ || path ~ /\.sh$/ || path ~ /\.md$/) {
        if (path ~ /\.yaml$/) icon = "📄"
        else if (path ~ /\.sh$/) icon = "🔧"
        else if (path ~ /\.md$/) icon = "📖"
        
        print prefix "├─ " icon " " path
    } else if (path != "") {
        print prefix "├─ 📁 " path "/"
    }
    
    prefix = ""
}
' 

echo ""
echo "════════════════════════════════════════════════════════════════════════════════"
echo ""
echo "📊 FILE SUMMARY"
echo ""

YAML_COUNT=$(find "$SCRIPT_DIR" -name "*.yaml" | wc -l)
SCRIPT_COUNT=$(find "$SCRIPT_DIR" -name "*.sh" | wc -l)
DOC_COUNT=$(find "$SCRIPT_DIR" -name "*.md" | wc -l)
TOTAL_COUNT=$((YAML_COUNT + SCRIPT_COUNT + DOC_COUNT))

echo "  Kubernetes Manifests (YAML):  $YAML_COUNT files"
echo "  Automation Scripts (SH):       $SCRIPT_COUNT files"
echo "  Documentation (MD):           $DOC_COUNT files"
echo "  ────────────────────────────────────────"
echo "  TOTAL FILES:                   $TOTAL_COUNT files"
echo ""

echo "════════════════════════════════════════════════════════════════════════════════"
echo ""
echo "📋 DETAILED FILE LISTING"
echo ""

echo "📁 Kubernetes Manifests (YAML Files)"
echo "─────────────────────────────────────"
find "$SCRIPT_DIR" -name "*.yaml" -type f | sort | while read file; do
    rel_path="${file#$SCRIPT_DIR/}"
    filesize=$(wc -l < "$file")
    echo "  📄 $rel_path ($filesize lines)"
done
echo ""

echo "🔧 Automation Scripts (Shell Scripts)"
echo "──────────────────────────────────────"
find "$SCRIPT_DIR" -name "*.sh" -type f | sort | while read file; do
    rel_path="${file#$SCRIPT_DIR/}"
    filesize=$(wc -l < "$file")
    echo "  🔧 $rel_path ($filesize lines)"
done
echo ""

echo "📖 Documentation Files (Markdown)"
echo "─────────────────────────────────"
find "$SCRIPT_DIR" -name "*.md" -type f | sort | while read file; do
    rel_path="${file#$SCRIPT_DIR/}"
    filesize=$(wc -l < "$file")
    echo "  📖 $rel_path ($filesize lines)"
done
echo ""

echo "════════════════════════════════════════════════════════════════════════════════"
echo ""
echo "📚 DOCUMENTATION GUIDE"
echo "────────────────────"
echo ""
echo "  1️⃣  START HERE: README.md"
echo "      → Navigation and index for all files"
echo ""
echo "  2️⃣  QUICK OVERVIEW: IMPLEMENTATION_SUMMARY.md"
echo "      → Executive summary and quick start (5 min read)"
echo ""
echo "  3️⃣  SETUP GUIDE: KUBERNETES_SETUP.md"
echo "      → Complete setup and troubleshooting guide (30 min read)"
echo ""
echo "  4️⃣  DETAILED REPORT: KUBERNETES_DEPLOYMENT_REPORT.md"
echo "      → Architecture and deployment details (20 min read)"
echo ""
echo "  5️⃣  QUICK REFERENCE: QUICK_REFERENCE.md"
echo "      → Essential commands and troubleshooting"
echo ""

echo "════════════════════════════════════════════════════════════════════════════════"
echo ""
echo "🚀 QUICK START"
echo "──────────────"
echo ""
echo "  1. Deploy everything:"
echo "     $ bash deploy.sh"
echo ""
echo "  2. Verify deployment:"
echo "     $ bash verify.sh"
echo ""
echo "  3. Check status:"
echo "     $ kubectl get all -n thethroneofgames"
echo ""
echo "  4. View logs:"
echo "     $ kubectl logs -n thethroneofgames -l app=usuarios-api -f"
echo ""

echo "════════════════════════════════════════════════════════════════════════════════"
echo ""
echo "✅ PHASE 4.2 STATUS: COMPLETE"
echo ""
echo "All components for Kubernetes orchestration have been implemented and documented."
echo "Ready for deployment to your Kubernetes cluster."
echo ""
echo "════════════════════════════════════════════════════════════════════════════════"
echo ""

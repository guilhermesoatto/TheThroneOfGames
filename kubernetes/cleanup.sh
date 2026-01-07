#!/bin/bash

# Kubernetes Cleanup Script for TheThroneOfGames
# This script removes all deployed resources

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

NAMESPACE="thethroneofgames"
MONITORING_NAMESPACE="thethroneofgames-monitoring"

echo -e "${BLUE}=========================================="
echo "TheThroneOfGames - Kubernetes Cleanup"
echo "==========================================${NC}"
echo ""

# Confirmation
echo -e "${RED}Warning: This will delete all resources in the following namespaces:${NC}"
echo "  - $NAMESPACE"
echo "  - $MONITORING_NAMESPACE"
echo ""
read -p "Are you sure you want to proceed? (yes/no): " confirmation

if [ "$confirmation" != "yes" ]; then
    echo -e "${YELLOW}Cleanup cancelled.${NC}"
    exit 0
fi

echo ""
echo -e "${YELLOW}Starting cleanup...${NC}"
echo ""

# Delete monitoring namespace
echo -e "${YELLOW}Deleting monitoring namespace...${NC}"
kubectl delete namespace $MONITORING_NAMESPACE --ignore-not-found=true 2>/dev/null || true
sleep 5
echo -e "${GREEN}✓ Monitoring namespace deleted${NC}"
echo ""

# Delete production namespace
echo -e "${YELLOW}Deleting production namespace...${NC}"
kubectl delete namespace $NAMESPACE --ignore-not-found=true 2>/dev/null || true
sleep 5
echo -e "${GREEN}✓ Production namespace deleted${NC}"
echo ""

# Verify cleanup
echo -e "${YELLOW}Verifying cleanup...${NC}"
echo ""

if ! kubectl get namespace $NAMESPACE &> /dev/null; then
    echo -e "${GREEN}✓ $NAMESPACE successfully removed${NC}"
else
    echo -e "${YELLOW}⚠ $NAMESPACE still exists (may be terminating)${NC}"
fi

if ! kubectl get namespace $MONITORING_NAMESPACE &> /dev/null; then
    echo -e "${GREEN}✓ $MONITORING_NAMESPACE successfully removed${NC}"
else
    echo -e "${YELLOW}⚠ $MONITORING_NAMESPACE still exists (may be terminating)${NC}"
fi

echo ""
echo -e "${BLUE}=========================================="
echo "Cleanup Complete"
echo "==========================================${NC}"
echo ""

echo -e "${YELLOW}Remaining Namespaces:${NC}"
kubectl get namespaces | grep -E "NAME|thethroneofgames" || echo "None"

#!/bin/bash

# Kubernetes Deployment Script for TheThroneOfGames
# This script deploys all microservices to Kubernetes

set -e  # Exit on error

echo "=========================================="
echo "TheThroneOfGames - Kubernetes Deployment"
echo "=========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="thethroneofgames"
MONITORING_NAMESPACE="thethroneofgames-monitoring"
KUBERNETES_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo -e "${YELLOW}Configuration:${NC}"
echo "Namespace: $NAMESPACE"
echo "Monitoring Namespace: $MONITORING_NAMESPACE"
echo "Kubernetes Directory: $KUBERNETES_DIR"
echo ""

# Function to print status
print_status() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ $1${NC}"
    else
        echo -e "${RED}✗ $1${NC}"
        exit 1
    fi
}

# Check if kubectl is installed
echo -e "${YELLOW}Checking prerequisites...${NC}"
if ! command -v kubectl &> /dev/null; then
    echo -e "${RED}kubectl is not installed. Please install kubectl first.${NC}"
    exit 1
fi
print_status "kubectl found"

# Check cluster connectivity
if ! kubectl cluster-info &> /dev/null; then
    echo -e "${RED}Cannot connect to Kubernetes cluster. Please configure kubectl.${NC}"
    exit 1
fi
print_status "Connected to Kubernetes cluster"

echo ""
echo -e "${YELLOW}Starting deployment...${NC}"
echo ""

# Step 1: Create namespaces
echo -e "${YELLOW}1. Creating namespaces...${NC}"
kubectl apply -f "$KUBERNETES_DIR/namespaces/namespace.yaml"
print_status "Namespaces created"
echo ""

# Step 2: Deploy database
echo -e "${YELLOW}2. Deploying SQL Server database...${NC}"
kubectl apply -f "$KUBERNETES_DIR/database/secrets.yaml"
kubectl apply -f "$KUBERNETES_DIR/database/mssql.yaml"
print_status "SQL Server deployed"
echo ""

# Step 3: Wait for database to be ready
echo -e "${YELLOW}3. Waiting for SQL Server to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app=mssql -n $NAMESPACE --timeout=300s 2>/dev/null || true
echo "Database initialization may take a few minutes..."
sleep 30
print_status "SQL Server ready"
echo ""

# Step 4: Deploy RabbitMQ
echo -e "${YELLOW}4. Deploying RabbitMQ message broker...${NC}"
kubectl apply -f "$KUBERNETES_DIR/rabbitmq/configmap.yaml"
kubectl apply -f "$KUBERNETES_DIR/rabbitmq/pvc.yaml"
kubectl apply -f "$KUBERNETES_DIR/rabbitmq/statefulset.yaml"
kubectl apply -f "$KUBERNETES_DIR/rabbitmq/service.yaml"
print_status "RabbitMQ deployed"
echo ""

# Step 5: Wait for RabbitMQ to be ready
echo -e "${YELLOW}5. Waiting for RabbitMQ to be ready...${NC}"
kubectl wait --for=condition=ready pod -l app=rabbitmq -n $NAMESPACE --timeout=300s 2>/dev/null || true
sleep 15
print_status "RabbitMQ ready"
echo ""

# Step 6: Deploy microservices
echo -e "${YELLOW}6. Deploying microservices...${NC}"
kubectl apply -f "$KUBERNETES_DIR/usuarios-api/usuarios-api.yaml"
kubectl apply -f "$KUBERNETES_DIR/catalogo-api/catalogo-api.yaml"
kubectl apply -f "$KUBERNETES_DIR/vendas-api/vendas-api.yaml"
print_status "Microservices deployed"
echo ""

# Step 7: Wait for microservices to be ready
echo -e "${YELLOW}7. Waiting for microservices to be ready...${NC}"
kubectl wait --for=condition=available --timeout=300s deployment/usuarios-api -n $NAMESPACE 2>/dev/null || echo "Usuarios API still starting..."
kubectl wait --for=condition=available --timeout=300s deployment/catalogo-api -n $NAMESPACE 2>/dev/null || echo "Catalogo API still starting..."
kubectl wait --for=condition=available --timeout=300s deployment/vendas-api -n $NAMESPACE 2>/dev/null || echo "Vendas API still starting..."
sleep 15
print_status "Microservices ready"
echo ""

# Step 8: Deploy Ingress
echo -e "${YELLOW}8. Deploying Ingress controller...${NC}"
kubectl apply -f "$KUBERNETES_DIR/ingress/ingress.yaml"
print_status "Ingress deployed"
echo ""

# Step 9: Deploy Monitoring
echo -e "${YELLOW}9. Deploying Prometheus monitoring...${NC}"
kubectl apply -f "$KUBERNETES_DIR/monitoring/prometheus.yaml"
print_status "Prometheus deployed"
echo ""

echo -e "${GREEN}=========================================="
echo "Deployment completed successfully!"
echo "==========================================${NC}"
echo ""

# Display deployment summary
echo -e "${YELLOW}Deployment Summary:${NC}"
echo ""
echo "Namespaces:"
kubectl get namespaces | grep thethroneofgames
echo ""

echo "Database Status:"
kubectl get pods -n $NAMESPACE -l app=mssql
echo ""

echo "Message Broker Status:"
kubectl get pods -n $NAMESPACE -l app=rabbitmq
echo ""

echo "Microservices Status:"
kubectl get pods -n $NAMESPACE -l tier=api
echo ""

echo "Services:"
kubectl get svc -n $NAMESPACE
echo ""

echo "Ingress:"
kubectl get ingress -n $NAMESPACE
echo ""

echo -e "${YELLOW}Useful Commands:${NC}"
echo ""
echo "View all resources:"
echo "  kubectl get all -n $NAMESPACE"
echo ""
echo "View pod logs:"
echo "  kubectl logs -n $NAMESPACE <pod-name>"
echo ""
echo "Port forward to access services locally:"
echo "  kubectl port-forward -n $NAMESPACE svc/usuarios-api-service 8001:80"
echo "  kubectl port-forward -n $NAMESPACE svc/mssql-service 1433:1433"
echo "  kubectl port-forward -n $NAMESPACE svc/rabbitmq-service 15672:15672"
echo ""
echo "Monitor deployments:"
echo "  kubectl get deployment -n $NAMESPACE -w"
echo ""
echo "Check HPA status:"
echo "  kubectl get hpa -n $NAMESPACE"
echo ""
echo -e "${YELLOW}Documentation:${NC}"
echo "For more information, see: $KUBERNETES_DIR/KUBERNETES_SETUP.md"

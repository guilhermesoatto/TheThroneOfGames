#!/bin/bash

# Kubernetes Verification Script for TheThroneOfGames
# This script verifies the deployment status and health of all components

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
echo "TheThroneOfGames - Kubernetes Verification"
echo "==========================================${NC}"
echo ""

# Function to check status
check_resource() {
    local resource_type=$1
    local resource_name=$2
    local namespace=$3
    
    if kubectl get $resource_type $resource_name -n $namespace &> /dev/null; then
        echo -e "${GREEN}✓ $resource_name exists${NC}"
        return 0
    else
        echo -e "${RED}✗ $resource_name not found${NC}"
        return 1
    fi
}

# Function to check pod status
check_pod_status() {
    local pod_name=$1
    local namespace=$2
    local status=$(kubectl get pod $pod_name -n $namespace -o jsonpath='{.status.phase}' 2>/dev/null)
    
    if [ "$status" = "Running" ]; then
        echo -e "${GREEN}✓ $pod_name is Running${NC}"
        return 0
    else
        echo -e "${RED}✗ $pod_name is $status${NC}"
        return 1
    fi
}

# Function to check deployment replicas
check_deployment_replicas() {
    local deployment=$1
    local namespace=$2
    local desired=$(kubectl get deployment $deployment -n $namespace -o jsonpath='{.spec.replicas}' 2>/dev/null)
    local ready=$(kubectl get deployment $deployment -n $namespace -o jsonpath='{.status.readyReplicas}' 2>/dev/null)
    
    if [ "$desired" = "$ready" ]; then
        echo -e "${GREEN}✓ $deployment: $ready/$desired replicas ready${NC}"
        return 0
    else
        echo -e "${YELLOW}⚠ $deployment: $ready/$desired replicas ready (still scaling)${NC}"
        return 0
    fi
}

echo -e "${YELLOW}1. Checking Namespaces${NC}"
echo ""
check_resource namespace $NAMESPACE default || true
check_resource namespace $MONITORING_NAMESPACE default || true
echo ""

echo -e "${YELLOW}2. Checking Database (SQL Server)${NC}"
echo ""
check_resource statefulset mssql $NAMESPACE || true
check_pod_status mssql-0 $NAMESPACE || true
echo ""

echo -e "${YELLOW}3. Checking Message Broker (RabbitMQ)${NC}"
echo ""
check_resource statefulset rabbitmq $NAMESPACE || true
check_pod_status rabbitmq-0 $NAMESPACE || true
echo ""

echo -e "${YELLOW}4. Checking Microservices${NC}"
echo ""
echo "Usuarios API:"
check_deployment_replicas usuarios-api $NAMESPACE || true
kubectl get pods -n $NAMESPACE -l app=usuarios-api --no-headers | head -3 || true
echo ""

echo "Catalogo API:"
check_deployment_replicas catalogo-api $NAMESPACE || true
kubectl get pods -n $NAMESPACE -l app=catalogo-api --no-headers | head -3 || true
echo ""

echo "Vendas API:"
check_deployment_replicas vendas-api $NAMESPACE || true
kubectl get pods -n $NAMESPACE -l app=vendas-api --no-headers | head -3 || true
echo ""

echo -e "${YELLOW}5. Checking Services${NC}"
echo ""
kubectl get svc -n $NAMESPACE --no-headers | while read line; do
    service_name=$(echo $line | awk '{print $1}')
    echo -e "${GREEN}✓ $service_name${NC}"
done
echo ""

echo -e "${YELLOW}6. Checking Ingress${NC}"
echo ""
check_resource ingress thethroneofgames-ingress $NAMESPACE || true
echo ""

echo -e "${YELLOW}7. Checking Persistent Volumes${NC}"
echo ""
echo "Persistent Volume Claims:"
kubectl get pvc -n $NAMESPACE --no-headers | while read line; do
    pvc_name=$(echo $line | awk '{print $1}')
    status=$(echo $line | awk '{print $2}')
    if [ "$status" = "Bound" ]; then
        echo -e "${GREEN}✓ $pvc_name: $status${NC}"
    else
        echo -e "${YELLOW}⚠ $pvc_name: $status${NC}"
    fi
done
echo ""

echo -e "${YELLOW}8. Checking ConfigMaps${NC}"
echo ""
kubectl get configmap -n $NAMESPACE --no-headers | while read line; do
    configmap_name=$(echo $line | awk '{print $1}')
    echo -e "${GREEN}✓ $configmap_name${NC}"
done
echo ""

echo -e "${YELLOW}9. Checking Secrets${NC}"
echo ""
kubectl get secrets -n $NAMESPACE --no-headers | grep -v "default-token" | while read line; do
    secret_name=$(echo $line | awk '{print $1}')
    echo -e "${GREEN}✓ $secret_name${NC}"
done
echo ""

echo -e "${YELLOW}10. Checking HorizontalPodAutoscalers${NC}"
echo ""
kubectl get hpa -n $NAMESPACE --no-headers | while read line; do
    hpa_name=$(echo $line | awk '{print $1}')
    reference=$(echo $line | awk '{print $2}')
    targets=$(echo $line | awk '{print $3}')
    minpods=$(echo $line | awk '{print $4}')
    maxpods=$(echo $line | awk '{print $5}')
    echo -e "${GREEN}✓ $hpa_name: $minpods-$maxpods replicas, targets: $targets${NC}"
done
echo ""

echo -e "${YELLOW}11. Checking Monitoring${NC}"
echo ""
check_resource deployment prometheus $MONITORING_NAMESPACE || true
echo ""

echo -e "${YELLOW}12. Health Check Summary${NC}"
echo ""

# Count pod status
total_pods=$(kubectl get pods -n $NAMESPACE --no-headers 2>/dev/null | wc -l)
running_pods=$(kubectl get pods -n $NAMESPACE --field-selector=status.phase=Running --no-headers 2>/dev/null | wc -l)

echo "Pods in $NAMESPACE:"
echo -e "  Total: $total_pods"
echo -e "  Running: $running_pods"

if [ $total_pods -eq $running_pods ]; then
    echo -e "${GREEN}✓ All pods are running${NC}"
else
    echo -e "${YELLOW}⚠ Some pods are not running${NC}"
fi

echo ""
echo -e "${BLUE}=========================================="
echo "Verification Complete"
echo "==========================================${NC}"
echo ""

echo -e "${YELLOW}Quick Links:${NC}"
echo ""
echo "Full Resource List:"
echo "  kubectl get all -n $NAMESPACE"
echo ""
echo "Pod Status:"
echo "  kubectl get pods -n $NAMESPACE -o wide"
echo ""
echo "Recent Events:"
echo "  kubectl get events -n $NAMESPACE --sort-by='.lastTimestamp'"
echo ""
echo "Pod Logs:"
echo "  kubectl logs -n $NAMESPACE -l app=usuarios-api --tail=50"
echo ""
echo "Resource Usage:"
echo "  kubectl top pods -n $NAMESPACE"
echo "  kubectl top nodes"

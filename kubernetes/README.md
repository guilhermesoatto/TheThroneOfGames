# 🚀 The Throne of Games - Kubernetes Platform Index

## 📖 Documentation Navigator

### 🎯 Start Here
1. **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** ⭐
   - Executive summary of Phase 4.2
   - Project status and deliverables
   - Quick start guide
   - 5-minute read

### 📚 Complete Guides

2. **[KUBERNETES_SETUP.md](KUBERNETES_SETUP.md)** - Full Documentation
   - Prerequisites and installation
   - Detailed component description
   - Configuration management
   - Troubleshooting guide
   - 30-minute read

3. **[KUBERNETES_DEPLOYMENT_REPORT.md](KUBERNETES_DEPLOYMENT_REPORT.md)** - Detailed Report
   - Architecture overview
   - Component specifications
   - Performance metrics
   - Production checklist
   - 20-minute read

4. **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Command Reference
   - Essential kubectl commands
   - Port forwarding setup
   - Debugging procedures
   - Common troubleshooting
   - Reference guide

---

## 🏗️ Kubernetes Manifests Structure

```
kubernetes/
│
├── 📄 IMPLEMENTATION_SUMMARY.md       ← START HERE
├── 📄 KUBERNETES_SETUP.md              ← Full documentation
├── 📄 KUBERNETES_DEPLOYMENT_REPORT.md  ← Detailed report
├── 📄 QUICK_REFERENCE.md               ← Command reference
├── 📄 README.md                        ← This file
│
├── 🔧 Deploy Scripts
│   ├── deploy.sh                       ← One-command deployment
│   ├── verify.sh                       ← Verify deployment status
│   └── cleanup.sh                      ← Remove all resources
│
├── 📦 Kubernetes Manifests
│   ├── kustomization.yaml              ← Orchestrate all resources
│   │
│   ├── namespaces/
│   │   └── namespace.yaml              ← Production & monitoring namespaces
│   │
│   ├── database/
│   │   ├── mssql.yaml                  ← SQL Server StatefulSet
│   │   └── secrets.yaml                ← DB credentials & RabbitMQ config
│   │
│   ├── rabbitmq/
│   │   ├── configmap.yaml              ← RabbitMQ configuration
│   │   ├── pvc.yaml                    ← 5Gi persistent volume
│   │   ├── statefulset.yaml            ← RabbitMQ deployment
│   │   └── service.yaml                ← Services (ClusterIP + LB)
│   │
│   ├── usuarios-api/
│   │   └── usuarios-api.yaml           ← Deployment + Service + HPA
│   │
│   ├── catalogo-api/
│   │   └── catalogo-api.yaml           ← Deployment + Service + HPA
│   │
│   ├── vendas-api/
│   │   └── vendas-api.yaml             ← Deployment + Service + HPA
│   │
│   ├── ingress/
│   │   ├── ingress.yaml                ← NGINX Ingress + policies
│   │   └── secrets.yaml                ← JWT & TLS secrets
│   │
│   └── monitoring/
│       └── prometheus.yaml             ← Prometheus monitoring stack
```

---

## 🚀 Quick Commands

### Deploy Everything
```bash
cd kubernetes/
bash deploy.sh
```

### Verify Deployment
```bash
bash kubernetes/verify.sh
```

### Access Services Locally
```bash
# Usuarios API (port 8001)
kubectl port-forward -n thethroneofgames svc/usuarios-api-service 8001:80

# Database (port 1433)
kubectl port-forward -n thethroneofgames svc/mssql-service 1433:1433

# RabbitMQ Management (port 15672)
kubectl port-forward -n thethroneofgames svc/rabbitmq-service 15672:15672

# Prometheus (port 9090)
kubectl port-forward -n thethroneofgames-monitoring svc/prometheus-service 9090:9090
```

### Monitor Status
```bash
kubectl get all -n thethroneofgames
kubectl get hpa -n thethroneofgames
kubectl top pods -n thethroneofgames
```

### View Logs
```bash
kubectl logs -n thethroneofgames -l app=usuarios-api -f
```

### Cleanup
```bash
bash kubernetes/cleanup.sh
```

---

## 📊 What's Deployed

### Infrastructure
- ✅ SQL Server 2019 Database (StatefulSet, 10Gi storage)
- ✅ RabbitMQ 3.12 Message Broker (StatefulSet, 5Gi storage)
- ✅ Prometheus Monitoring (Deployment, 7-day retention)

### Microservices (3 APIs)
- ✅ Usuarios API (3-10 replicas with auto-scaling)
- ✅ Catalogo API (3-10 replicas with auto-scaling)
- ✅ Vendas API (3-10 replicas with auto-scaling)

### Networking
- ✅ NGINX Ingress with path-based routing
- ✅ Network policies for pod communication
- ✅ Kubernetes DNS for service discovery

### Features
- ✅ High Availability (multi-replica deployments)
- ✅ Auto-Scaling (CPU/Memory-based HPA)
- ✅ Health Checks (Liveness & Readiness probes)
- ✅ Persistent Storage (PVC for database & message broker)
- ✅ Configuration Management (ConfigMaps & Secrets)
- ✅ Service Discovery (Kubernetes DNS)
- ✅ Monitoring & Observability (Prometheus)

---

## 🔍 Key Files Reference

### Deployment Automation
| File | Purpose |
|------|---------|
| `deploy.sh` | Automated deployment script |
| `verify.sh` | Status verification script |
| `cleanup.sh` | Resource cleanup script |

### Core Infrastructure
| File | Purpose |
|------|---------|
| `namespaces/namespace.yaml` | Kubernetes namespaces |
| `database/mssql.yaml` | SQL Server deployment |
| `database/secrets.yaml` | Credentials & config |
| `rabbitmq/statefulset.yaml` | Message broker |

### Microservices
| File | Purpose |
|------|---------|
| `usuarios-api/usuarios-api.yaml` | Usuarios service |
| `catalogo-api/catalogo-api.yaml` | Catalogo service |
| `vendas-api/vendas-api.yaml` | Vendas service |

### Networking & Monitoring
| File | Purpose |
|------|---------|
| `ingress/ingress.yaml` | NGINX Ingress routing |
| `ingress/secrets.yaml` | TLS & JWT secrets |
| `monitoring/prometheus.yaml` | Prometheus stack |

---

## 🎯 Use Cases

### I want to...

#### Deploy the entire platform
→ Read [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) then run `bash deploy.sh`

#### Understand the architecture
→ Read [KUBERNETES_DEPLOYMENT_REPORT.md](KUBERNETES_DEPLOYMENT_REPORT.md#architecture-overview)

#### Debug an issue
→ See [QUICK_REFERENCE.md](QUICK_REFERENCE.md#-troubleshooting)

#### Access a service locally
→ Use port-forward commands in [QUICK_REFERENCE.md](QUICK_REFERENCE.md#-service-access)

#### Monitor the deployment
→ Run `bash verify.sh` or check commands in [QUICK_REFERENCE.md](QUICK_REFERENCE.md#-service-access)

#### Scale a service
→ See scaling commands in [QUICK_REFERENCE.md](QUICK_REFERENCE.md#scaling--autoscaling)

#### Update configuration
→ Read [KUBERNETES_SETUP.md](KUBERNETES_SETUP.md#configuration-management)

#### View logs
→ See commands in [QUICK_REFERENCE.md](QUICK_REFERENCE.md#-logs--debugging)

#### Production preparation
→ Review [KUBERNETES_DEPLOYMENT_REPORT.md](KUBERNETES_DEPLOYMENT_REPORT.md#production-checklist)

---

## 📋 Prerequisites

### Requirements
- Kubernetes cluster (1.20+) - Minikube, Docker Desktop, EKS, AKS, GKE
- kubectl CLI configured to access cluster
- Docker images built:
  - `usuarios-api:latest`
  - `catalogo-api:latest`
  - `vendas-api:latest`
- Storage provisioner (local-path or cloud storage)

### Optional
- NGINX Ingress Controller (for external access)
- cert-manager (for TLS certificates)
- Metrics server (for HPA)

---

## ⚡ Quick Start (3 Minutes)

### Step 1: Build Docker Images
```bash
cd ../
docker build -t usuarios-api:latest ./TheThroneOfGames.Usuarios.API
docker build -t catalogo-api:latest ./TheThroneOfGames.Catalogo.API
docker build -t vendas-api:latest ./TheThroneOfGames.Vendas.API
```

### Step 2: Deploy to Kubernetes
```bash
cd kubernetes/
bash deploy.sh
```

### Step 3: Verify Deployment
```bash
bash verify.sh
```

### Step 4: Access Services
```bash
# Port forward to Usuarios API
kubectl port-forward -n thethroneofgames svc/usuarios-api-service 8001:80

# Open in browser: http://localhost:8001/swagger
```

---

## 🔐 Security Notes

Before deploying to production, update:

1. **JWT Secret** - Change strong random key
2. **Database Password** - Change to strong password
3. **RabbitMQ Credentials** - Change default guest/guest
4. **TLS Certificate** - Add valid SSL certificate
5. **Network Policies** - Review pod communication rules

See [KUBERNETES_SETUP.md](KUBERNETES_SETUP.md#security) for details.

---

## 📊 Resource Requirements

### Total Resources (All 3 APIs @ max 10 replicas)
- CPU: 15 cores
- Memory: 15.36 GB

### Per Microservice Pod
- CPU Request: 250m, Limit: 500m
- Memory Request: 256Mi, Limit: 512Mi

### Database (SQL Server)
- CPU Request: 2000m, Limit: 4000m
- Memory Request: 2Gi, Limit: 4Gi
- Storage: 10Gi

### Message Broker (RabbitMQ)
- CPU Request: 1000m, Limit: 2000m
- Memory Request: 512Mi, Limit: 1Gi
- Storage: 5Gi

---

## 📞 Support & Troubleshooting

### Common Issues

**Q: Pods not starting?**
A: Check logs with `kubectl logs -n thethroneofgames <pod-name>`

**Q: Database connection timeout?**
A: Verify with `kubectl describe pod -n thethroneofgames mssql-0`

**Q: Services not communicating?**
A: Test DNS with `kubectl run -it --rm debug --image=busybox -- nslookup mssql-service`

See [QUICK_REFERENCE.md](QUICK_REFERENCE.md#-troubleshooting) for more solutions.

---

## 🎓 Next Steps

### Learn More
1. Read [KUBERNETES_SETUP.md](KUBERNETES_SETUP.md) for complete documentation
2. Review [KUBERNETES_DEPLOYMENT_REPORT.md](KUBERNETES_DEPLOYMENT_REPORT.md) for architecture
3. Keep [QUICK_REFERENCE.md](QUICK_REFERENCE.md) handy for commands

### Deploy
1. Prepare Docker images
2. Verify prerequisites
3. Run `bash deploy.sh`
4. Run `bash verify.sh`

### Monitor
1. Use `kubectl` commands to check status
2. View logs with `kubectl logs`
3. Access Prometheus on port 9090

### Scale & Update
1. Monitor HPA with `kubectl get hpa`
2. Update images with `kubectl set image`
3. Rollback if needed with `kubectl rollout undo`

---

## 📚 Documentation Map

```
📖 Documentation Files

├── 🎯 IMPLEMENTATION_SUMMARY.md
│   ├─ Executive Summary (5 min)
│   ├─ Quick Start (2 min)
│   ├─ Key Features (3 min)
│   └─ Verification Checklist (1 min)
│
├── 📖 KUBERNETES_SETUP.md
│   ├─ Prerequisites & Installation (10 min)
│   ├─ Component Details (15 min)
│   ├─ Configuration Management (5 min)
│   ├─ Troubleshooting Guide (10 min)
│   └─ Production Checklist (5 min)
│
├── 📊 KUBERNETES_DEPLOYMENT_REPORT.md
│   ├─ Architecture Overview (10 min)
│   ├─ File Structure & Manifests (10 min)
│   ├─ Deployment Instructions (5 min)
│   ├─ Performance Metrics (5 min)
│   └─ Advanced Configuration (10 min)
│
├── ⚡ QUICK_REFERENCE.md
│   ├─ Quick Start (2 min)
│   ├─ Essential Commands (5 min)
│   ├─ Port Forwarding (3 min)
│   ├─ Troubleshooting (10 min)
│   └─ Common Scenarios (5 min)
│
└── 📝 README.md (THIS FILE)
    └─ Navigation & Index
```

---

## ✅ Deployment Checklist

- [ ] Docker images built and ready
- [ ] Prerequisites verified
- [ ] Read IMPLEMENTATION_SUMMARY.md
- [ ] Run `bash deploy.sh`
- [ ] Run `bash verify.sh`
- [ ] All pods running
- [ ] Services accessible
- [ ] Database connected
- [ ] RabbitMQ operational
- [ ] APIs responding
- [ ] Monitoring active
- [ ] Documentation reviewed

---

## 🏆 Phase 4.2 Completion Status

| Component | Status | Files |
|-----------|--------|-------|
| Kubernetes Setup | ✅ Complete | 15 manifest files |
| Automation Scripts | ✅ Complete | 3 shell scripts |
| Documentation | ✅ Complete | 4 comprehensive guides |
| Infrastructure | ✅ Complete | Database, Message Broker |
| Microservices | ✅ Complete | 3 APIs with HPA |
| Monitoring | ✅ Complete | Prometheus stack |
| Networking | ✅ Complete | Ingress + Network Policies |
| Security | ✅ Complete | Secrets + RBAC |
| **Overall** | **✅ COMPLETE** | **21 files** |

---

## 📞 Getting Help

1. **Quick answers?** → Check [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
2. **Setup help?** → Read [KUBERNETES_SETUP.md](KUBERNETES_SETUP.md)
3. **Architecture questions?** → See [KUBERNETES_DEPLOYMENT_REPORT.md](KUBERNETES_DEPLOYMENT_REPORT.md)
4. **Getting started?** → Read [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)

---

**🎉 Welcome to The Throne of Games Kubernetes Platform! 🎉**

**Status**: ✅ Ready for Deployment | **Version**: 1.0 | **Phase**: 4.2 Complete

For deployment instructions, start with [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)

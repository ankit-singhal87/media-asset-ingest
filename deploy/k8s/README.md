# Kubernetes Readiness Assets

This directory contains static Kubernetes assets for the intended local and
Azure-ready runtime shape. They are readiness assets, not a production release
bundle.

## Boundary

- Manifests are safe to review and validate locally.
- Services use `ClusterIP`; no load balancer or ingress is created.
- Images use placeholder repository names and tags.
- Azure Service Bus and PostgreSQL credentials are referenced by Kubernetes
  Secret names only. Real secret values, kubeconfigs, subscription ids, and
  Terraform state stay outside the repository.
- Paid cloud execution requires explicit approval before any `kubectl`, `az`,
  Helm, or Dapr command targets a paid cluster or Azure resource.

## Apply Order

Use these commands only against an approved local cluster or approved Azure
cluster context:

```bash
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -k deploy/k8s
kubectl apply -k deploy/dapr/k8s
```

Before applying to a cluster, copy `secrets.example.yaml` outside the repository
or manage equivalent secrets through the approved environment's secret manager.
Do not commit the filled-in copy.

## Static Validation

The expected validation for this slice is static review:

```bash
kubectl kustomize deploy/k8s
kubectl kustomize deploy/dapr/k8s
make docs-check
git diff --check
```

Optional local validation, only when a disposable local cluster is selected and
reachable:

```bash
kubectl apply --dry-run=client -k deploy/k8s
kubectl apply --dry-run=client -k deploy/dapr/k8s
```

Do not run cloud validation without explicit approval.

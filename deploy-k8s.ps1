param(
    [string]$Command = "help"
)

$NAMESPACE = "tech-challenge-game"

function Deploy {
    Write-Host "Iniciando deploy..." -ForegroundColor Green
    kubectl apply -f k8s/namespace.yaml
    kubectl apply -f k8s/configmap.yaml
    kubectl apply -f k8s/secrets.yaml
    kubectl apply -f k8s/newrelic-secrets.yaml
    kubectl apply -f k8s/postgres-pvc.yaml
    kubectl apply -f k8s/deployment.yaml
    kubectl apply -f k8s/service.yaml
    kubectl apply -f k8s/ingress.yaml
    
    Write-Host "Aguardando PostgreSQL..." -ForegroundColor Blue
    kubectl wait --for=condition=ready pod -l app=postgres-db -n $NAMESPACE --timeout=300s
    
    Write-Host "Aguardando Game API..." -ForegroundColor Blue
    kubectl wait --for=condition=ready pod -l app=game-api -n $NAMESPACE --timeout=300s
    
    Write-Host "Deploy concluido!" -ForegroundColor Green
}

function Status {
    Write-Host "Status:" -ForegroundColor Green
    kubectl get pods -n $NAMESPACE -o wide
    kubectl get svc -n $NAMESPACE
    kubectl get ingress -n $NAMESPACE
}

function Logs {
    Write-Host "Logs API:" -ForegroundColor Blue
    kubectl logs -l app=game-api -n $NAMESPACE --tail=20
    Write-Host "Logs PostgreSQL:" -ForegroundColor Blue
    kubectl logs -l app=postgres-db -n $NAMESPACE --tail=20
}

function Cleanup {
    $confirmation = Read-Host "Remover todos os recursos? (y/N)"
    if ($confirmation -eq 'y') {
        kubectl delete -f k8s/ -n $NAMESPACE --ignore-not-found=true
        kubectl delete namespace $NAMESPACE --ignore-not-found=true
        Write-Host "Cleanup concluido!" -ForegroundColor Green
    }
}

switch ($Command) {
    "deploy" { Deploy }
    "status" { Status }
    "logs" { Logs }
    "cleanup" { Cleanup }
    default { 
        Write-Host "Comandos: deploy, status, logs, cleanup"
        Write-Host "Uso: .\deploy-k8s.ps1 deploy"
    }
}

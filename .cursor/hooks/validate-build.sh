#!/usr/bin/env bash
# =============================================================================
# Hook "stop" do Cursor - valida o build antes de considerar a tarefa concluída.
#
# - backend/ alterado  -> dotnet build backend/MiniGestaoPedidos.sln
# - frontend/ alterado -> npm run build --prefix frontend (se houver package.json
#                         e node_modules)
#
# O evento "stop" não bloqueia por exit code: quando o build falha, o hook devolve
# {"followup_message": "..."} e o Cursor reenvia a mensagem ao agente para que ele
# corrija o erro (limitado por "loop_limit" no hooks.json).
#
# Dependências: apenas bash, git (opcional), dotnet e npm. Sem jq/python.
# Log da última execução: .cursor/hooks/.last-build.log (ignorado pelo Git).
# =============================================================================
set -u

input="$(cat || true)"

# Só valida quando o agente terminou normalmente (ignora "aborted" e "error").
status="$(printf '%s' "$input" | sed -n 's/.*"status"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"
if [ -n "$status" ] && [ "$status" != "completed" ]; then
  echo '{}'
  exit 0
fi

# Raiz do projeto = duas pastas acima deste script (.cursor/hooks -> raiz).
root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)" || { echo '{}'; exit 0; }
cd "$root" || { echo '{}'; exit 0; }

log=".cursor/hooks/.last-build.log"
: > "$log"

has_changes() {
  # Sem Git, assume que houve mudança para não deixar de validar.
  if git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    [ -n "$(git status --porcelain -- "$1" 2>/dev/null)" ]
  else
    return 0
  fi
}

json_escape() {
  # Escapa para string JSON: remove controles, troca tab por espaço e quebra de linha por \n.
  printf '%s' "$1" \
    | tr '\t' ' ' \
    | tr -d '\000-\010\013-\037' \
    | sed -e 's/\\/\\\\/g' -e 's/"/\\"/g' \
    | awk 'BEGIN { ORS = "\\n" } { print }'
}

failures=""

run_build() {
  # $1 = nome amigável; demais argumentos = comando
  local name="$1"
  shift
  local cmd="$*"
  local out code
  echo "== $name: $cmd ==" >> "$log"
  out="$("$@" 2>&1)"
  code=$?
  printf '%s\n' "$out" >> "$log"
  if [ "$code" -ne 0 ]; then
    failures="${failures}
[$name] falhou ($cmd). Últimas linhas:
$(printf '%s\n' "$out" | tail -n 30)
"
  fi
}

# ---------------- Backend ----------------
if [ -d backend ] && has_changes backend; then
  if command -v dotnet >/dev/null 2>&1; then
    run_build "backend" dotnet build backend/MiniGestaoPedidos.sln --nologo -v q -clp:ErrorsOnly
  else
    echo "validate-build: 'dotnet' não encontrado no PATH; build do backend ignorado." >&2
  fi
fi

# ---------------- Frontend ----------------
if [ -f frontend/package.json ] && has_changes frontend; then
  if ! command -v npm >/dev/null 2>&1; then
    echo "validate-build: 'npm' não encontrado no PATH; build do frontend ignorado." >&2
  elif [ ! -d frontend/node_modules ]; then
    echo "validate-build: frontend/node_modules ausente (rode 'npm install --prefix frontend'); build do frontend ignorado." >&2
  else
    run_build "frontend" npm run build --prefix frontend
  fi
fi

if [ -n "$failures" ]; then
  message="A validação de build falhou. Corrija os erros abaixo e conclua a tarefa novamente (log completo em .cursor/hooks/.last-build.log).
${failures}"
  printf '{"followup_message": "%s"}\n' "$(json_escape "$message")"
  exit 0
fi

echo '{}'
exit 0

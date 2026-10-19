#!/usr/bin/env bash
#
# Builds and starts Stanley.
#
#   scripts/up.sh                 build the image, replace the bot container, wait for Ready
#   scripts/up.sh postgres        the same for one service only
#   STANLEY_UP_TIMEOUT=240 ...    allow longer than two minutes for the first build
#
# `docker compose up -d` returns as soon as the container is started, which is before the bot
# has applied the migrations, logged in, or registered a single command. A deploy that looks
# successful while the bot is in a crash loop is the failure worth catching, so this waits for
# the line Discord.Net prints when the gateway is live and fails loudly otherwise.
set -euo pipefail

# Run from the repository root whatever directory the script was called from, so the relative
# paths in docker-compose.yml resolve.
cd "$(dirname "${BASH_SOURCE[0]}")/.."

readonly READY_LINE='[Gateway] Ready'
readonly TIMEOUT_SECONDS="${STANLEY_UP_TIMEOUT:-120}"

if [ ! -f .env ]; then
    echo "No .env in $(pwd)." >&2
    echo "Copy it and fill in the token and the database password:" >&2
    echo "    cp .env.example .env" >&2
    exit 1
fi

echo "==> Building and starting"
if [ "$#" -gt 0 ]; then
    docker compose up -d --build --force-recreate "$@"
else
    docker compose up -d --build --force-recreate bot
fi

# Nothing to wait for when only the database was asked for.
if [ "$#" -gt 0 ] && ! printf '%s\n' "$@" | grep -qx 'bot'; then
    docker compose ps
    exit 0
fi

bot_container="$(docker compose ps --quiet bot)"
if [ -z "$bot_container" ]; then
    echo "The bot container was not created. See 'docker compose ps'." >&2
    exit 1
fi

echo "==> Waiting up to ${TIMEOUT_SECONDS}s for the bot to report $READY_LINE"
deadline=$((SECONDS + TIMEOUT_SECONDS))
while true; do
    if docker compose logs bot 2>/dev/null | grep -qF "$READY_LINE"; then
        break
    fi

    # A container that is no longer running has crashed: the token, the database or the
    # configuration. Its own last lines say which, so they are what gets printed.
    state="$(docker inspect --format '{{.State.Status}}' "$bot_container")"
    if [ "$state" != "running" ]; then
        echo "The bot stopped ($state). Its last 30 log lines:" >&2
        docker compose logs bot --tail 30 >&2
        exit 1
    fi

    if [ "$SECONDS" -ge "$deadline" ]; then
        echo "The bot did not report $READY_LINE within ${TIMEOUT_SECONDS}s." >&2
        echo "It is still running, so follow it with 'docker compose logs -f bot'." >&2
        docker compose logs bot --tail 30 >&2
        exit 1
    fi

    sleep 2
done

# The four lines that say a start went well: the schema, the login, where the commands landed,
# and the gateway. `|| true` because grep finding nothing must not fail the deploy.
echo "==> Up"
docker compose logs bot \
    | grep -E 'Migrations applied|schema is up to date|Registered [0-9]+ command|Gateway\] (Connected|Ready)' \
    | tail -n 6 \
    || true

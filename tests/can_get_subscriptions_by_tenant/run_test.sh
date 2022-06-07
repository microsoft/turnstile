#!/bin/bash

api_url=$1
api_key=$2

# All of the resources that this test needs to run are located in this folder. When called from ../e2e.sh,
# this script inherits ../e2e.sh's pwd so we can't really use "./" to refer to this folder's path. Instead,
# we use this super-helpful bit of shell to get this folder's path.

here=$(cd "$(dirname "$(readlink -f "$0")")" >/dev/null 2>&1 && pwd)

subscription1_json=$(cat "$here/subscription_1.json")
subscription2_json=$(cat "$here/subscription_2.json")

subscription1_id=$(echo "$subscription1_json" | jq -r ".subscription_id")
subscription2_id=$(echo "$subscription2_json" | jq -r ".subscription_id")

tenant_id=$(echo "$subscription1_json" | jq -r ".tenant_id")

echo "Running [can get all subscriptions by tenant] test..."

post1_url="$api_url/api/saas/subscriptions/$subscription1_id"

echo "POSTing new subscription [$subscription1_id] to [$post1_url]..."

post1_status_code=$(curl -s \
    -X POST \
    -H "Content-Type: application/json" \
    -H "x-functions-key: $api_key" \
    -d "$subscription1_json" \
    -o /dev/null \
    -w "%{http_code}" \
    "$post1_url")

if [[ $post1_status_code != "200" ]]; then
    echo "❌   POST subscription [$subscription1_id] to [$post1_url] failed with status code: [$post1_status_code]."
    exit 1
fi

post2_url="$api_url/api/saas/subscriptions/$subscription2_id"

echo "POSTing new subscription [$subscription2_id] to [$post2_url]..."

post2_status_code=$(curl -s \
    -X POST \
    -H "Content-Type: application/json" \
    -H "x-functions-key: $api_key" \
    -d "$subscription2_json" \
    -o /dev/null \
    -w "%{http_code}" \
    "$post2_url")

if [[ $post2_status_code != "200" ]]; then
    echo "❌   POST subscription [$subscription2_id] to [$post2_url] failed with status code: [$post2_status_code]."
    exit 1
fi

get_url="$api_url/api/saas/subscriptions?tenant_id=$tenant_id"

echo "GETting tenant [$tenant_id] subscriptions from [$get_url]..."

get_subscriptions_response=$(curl \
    -X GET \
    -H "Content-Type: application/json" \
    -H "x-functions-key: $api_key" \
    "$get_url")

subscription_ct=$(echo "$get_subscriptions_response" | jq ". | length")

if [[ $subscription_ct == 2 ]]; then
    echo "✔️   Tenant [$tenant_id] has [$subscription_ct] subscriptions; test passed."
    exit 0
else
    echo "❌   Test failed."
    exit 1
fi


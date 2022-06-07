#!/bin/bash

api_url=$1
api_key=$2

# All of the resources that this test needs to run are located in this folder. When called from ../e2e.sh,
# this script inherits ../e2e.sh's pwd so we can't really use "./" to refer to this folder's path. Instead,
# we use this super-helpful bit of shell to get this folder's path.

here=$(cd "$(dirname "$(readlink -f "$0")")" >/dev/null 2>&1 && pwd)

subscription_json=$(cat "$here/subscription.json")
subscription_id=$(echo "$subscription_json" | jq -r ".subscription_id")

echo "Running [can create subscription] test..."

url="$api_url/api/saas/subscriptions/$subscription_id"

echo "POSTing new subscription [$subscription_id] to [$url]..."

post_status_code=$(curl -s \
    -X POST \
    -H "Content-Type: application/json" \
    -H "x-functions-key: $api_key" \
    -d "$subscription_json" \
    -o /dev/null \
    -w "%{http_code}" \
    "$url")

if [[ $post_status_code == "200" ]]; then
    echo "GETting subscription [$subscription_id]..."

    get_response=$(curl \
        -X GET \
        -H "Content-Type: application/json" \
        -H "x-functions-key: $api_key" \
        "$url")

    projector="{subscription_id, subscription_name, tenant_id, tenant_name, offer_id, plan_id, state, admin_role_name, user_role_name, admin_name, admin_email, total_seats, is_being_configred, is_free_trial, is_setup_complete, is_test_subscription}"

    actual_subscription=$(echo "$get_response" | jq "$projector")
    expected_subscription=$(echo "$subscription_json" | jq "$projector")

    if [[ "$actual_subscription" == "$expected_subscription" ]]; then
        echo "✔️   Can create subscription; test passed."
        exit 0 # Test passed!
    else
        echo "❌   Can't create subscription; test failed."
        echo
        echo "Expected subscription is..."
        echo $(echo "$expected_subscription" | jq ".")
        echo
        echo "Actual subscription is..."
        echo $(echo "$actual_subscription" | jq ".")
        exit 1 # Test failed
    fi
else
    echo "❌   POST subscription [$subscription_id] to [$url] failed with status code: [$post_status_code]."
    exit 1 # Test failed
fi
#!/bin/bash

# Consider this a "private script" - it's designed to be run only by ../e2e.sh

passed=0 # Anything other than 0 indicates that this test did not pass.

api_url=$1
api_key=$2

script_path=$(cd "$(dirname "$(readlink -f "$0")")" >/dev/null 2>&1 && pwd)

echo "Looking for template subscription @ [$script_path/subscription.json]."

subscription_json=$(cat "./subscription.json")
subscription_id=$(echo "$subscription_json" | jq -r ".subscription_id")

echo
echo "🧪   Running [can create subscription] test..."
echo "--------------------------------------------------------------------------------------"
echo "This test verifies that a subscription can be created using the Turnstile API and that"
echo "same subscription can be accurately read back if needed."
echo

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
    get_response=$(curl \
        -X GET \
        -H "Content-Type: application/json" \
        -H "x-functions-key: $api_key" \
        "$url")

    projector="{subscription_id, subscription_name, tenant_id, tenant_name, offer_id, plan_id, state, admin_role_name, user_role_name, admin_name, admin_email, total_seats, is_being_configred, is_free_trial, is_setup_complete, is_test_subscription, created_utc, state_last_updated_utc}"

    actual_subscription=$(echo "$get_response" | jq "$projector")
    expected_subscription=$(echo "$subscription_json" | jq "$projector")

    if [[ "$actual_subscription" == "$expected_subscription" ]]; then
        echo "✔️   Can create subscription test passed."
    else
        echo "❌   Can create subscription test failed."
        echo
        echo "Expected subscription is..."
        echo $(echo "$expected_subscription" | jq)
        echo
        echo "Actual subscription is..."
        echo $(echo "$actual_subscription" | jq)
        passed=1
    fi
else
    echo "❌   POST subscription [$subscription_id] to [$post_url] failed with status code: [$post_status_code]."
    passed=1
fi

exit $passed
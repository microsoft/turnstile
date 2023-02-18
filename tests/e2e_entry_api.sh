#!/bin/bash

run_entry_api_test() {
    local api_base_url=$1
    local api_key=$2
    local subscription_json_path=$3
    local seat_request_json_path=$4
    local expected_seat_code=$5
    local test_name=$6

    local subscription_json=$(cat "$subscription_json_path")
    local seat_request_json=$(cat "$seat_request_json_path")

    local subscription_id=$(echo "$subscription_json" | jq -r ".subscription_id")
    local create_subscription_url="$api_base_url/saas/subscriptions/$subscription_id"

    local create_subscription_status_code=$(curl -s \
        -X POST \
        -H "Content-Type: application/json" \
        -H "x-functions-key: $api_key" \
        -d "$subscription_json" \
        -o /dev/null \
        -w "%{http_code}" \
        "$create_subscription_url")

    if [[ "$create_subscription_status_code" == "200" ]]; then
        echo "✔️   Entry API test subscription [$subscription_id] successfully created."

        local entry_url="$api_base_url/saas/subscriptions/$subscription_id/entry"

        local entry_response=$(curl \
            -X POST \
            -H "Content-Type: application/json" \
            -H "x-functions-key: $api_key" \
            -d "$seat_request_json" \
            "$entry_url")

        local actual_seat_code=$(echo "$entry_response" | jq -r ".seat_code")

        if [[ "$actual_seat_code" == "$expected_seat_code" ]]; then
            echo "✔️   [$test_name] entry API test passed."
            return 0
        else
            echo "❌   [$test_name] entry API test failed..."
            echo "❌   Expected seat result code is [$expected_seat_code] but actual code is [$actual_seat_code]."
            return 1
        fi
    else
        echo "❌   [$test_name] entry API test failed..."
        echo "❌   Unable to create entry API test subscription [$subscription_id]: [HTTP $create_subscription_status_code]."
        return 1     
    fi
}

run_subscription_not_found_entry_api_test() {
    local api_base_url=$1
    local api_key=$2

    local seat_request_json=$(cat "./models/entry_api_tests/subscription_not_found/seat_request.json")
    local entry_url="$api_base_url/saas/subscriptions/not_a_real_sub_id/entry"

    local entry_response=$(curl \
            -X POST \
            -H "Content-Type: application/json" \
            -H "x-functions-key: $api_key" \
            -d "$seat_request_json" \
            "$entry_url")

    local actual_seat_code=$(echo "$entry_response" | jq -r ".seat_code")

    if [[ "$actual_seat_code" == "subscription_not_found" ]]; then
        echo "✔️   [Subscription not found [subscription_not_found]] entry API test passed."
        return 0
    else
        echo "❌   [Subscription not found [subscription_not_found]] entry API test failed..."
        echo "❌   Expected seat result code is [subscription_not_found] but actual code is [$actual_seat_code]."
        return 1
    fi
}

run_entry_api_tests() {
    local api_base_url=$1
    local api_key=$2

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/access_denied/because_different_tenant/subscription.json" \
        "./models/entry_api_tests/access_denied/because_different_tenant/seat_request.json" \
        "access_denied" \
        "Access denied [access_denied] because requesting user belongs to a different tenant"

    [[ $? == 0 ]] || tests_failed=1

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/access_denied/because_user_not_in_role/subscription.json" \
        "./models/entry_api_tests/access_denied/because_user_not_in_role/seat_request.json" \
        "access_denied" \
        "Access denied [access_denied] because requesting user does not belong to the subscription's user role"

    [[ $? == 0 ]] || tests_failed=1

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/no_seats_available/subscription.json" \
        "./models/entry_api_tests/no_seats_available/seat_request.json" \
        "no_seats_available" \
        "No seats available [no_seats_available]"

    [[ $? == 0 ]] || tests_failed=1

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/seat_provided/subscription.json" \
        "./models/entry_api_tests/seat_provided/seat_request.json" \
        "seat_provided" \
        "Seat provided [seat_provided]"

    [[ $? == 0 ]] || tests_failed=1

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/subscription_canceled/subscription.json" \
        "./models/entry_api_tests/subscription_canceled/seat_request.json" \
        "subscription_canceled" \
        "Subscription canceled [subscription_canceled]"

    [[ $? == 0 ]] || tests_failed=1

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/subscription_not_ready/because_subscription_is_being_configured/subscription.json" \
        "./models/entry_api_tests/subscription_not_ready/because_subscription_is_being_configured/seat_request.json" \
        "subscription_not_ready" \
        "Subscription not ready [subscription_not_ready] because subscription is being configured"

    [[ $? == 0 ]] || tests_failed=1

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/subscription_not_ready/because_subscription_is_inactive/subscription.json" \
        "./models/entry_api_tests/subscription_not_ready/because_subscription_is_inactive/seat_request.json" \
        "subscription_not_ready" \
        "Subscription not ready [subscription_not_ready] because subscription is inactive"

    [[ $? == 0 ]] || tests_failed=1

    run_entry_api_test "$api_base_url" "$api_key" \
        "./models/entry_api_tests/subscription_suspended/subscription.json" \
        "./models/entry_api_tests/subscription_suspended/seat_request.json" \
        "subscription_suspended" \
        "Subscription suspended [subscription_suspended]"

    [[ $? == 0 ]] || tests_failed=1

    run_subscription_not_found_entry_api_test "$api_base_url" "$api_key"

    [[ $? == 0 ]] || tests_failed=1
    [[ -z $tests_failed ]] || return 1
}

api_base_url=$1
api_key=$2

echo "🧪   Running entry API tests..."

run_entry_api_tests "$api_base_url" "$api_key"

if [[ $? == 0 ]]; then
    echo "✔️   Entry API tests passed."
    exit 0
else
    echo "❌   Entry API tests failed."
    exit 1
fi
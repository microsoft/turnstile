#!/bin/bash

run_entry_api_test() {
    local api_base_url=$1
    local api_key=$2
    local subscription_json_path=$3
    local seat_request_json_path=$4
    local expected_seat_code=$5
    local test_name=$6

    
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

    [[ -z test_failed ]] || return 1
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
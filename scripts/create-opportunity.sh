# POST https://localhost:5001/api/openbooking/test-interface/datasets/uat-ci/opportunities

# Content-Type: "application/vnd.openactive.booking+json; version=1"
# X-OpenActive-Test-Client-Id: "booking-partner-1"
# X-OpenActive-Test-Seller-Id: "https://localhost:5001/api/identifiers/seller"
# {
#   "@type": "ScheduledSession",
#   "superEvent": {
#     "@type": "SessionSeries",
#     "organizer": {
#       "@type": "Organization",
#       "@id": "https://localhost:5001/api/identifiers/seller"
#     }
#   },
#   "@context": [
#     "https://openactive.io/",
#     "https://openactive.io/test-interface"
#   ],
#   "test:testOpportunityCriteria": "https://openactive.io/test-interface#TestOpportunityBookableInPast",
#   "test:testOpenBookingFlow": "https://openactive.io/test-interface#OpenBookingSimpleFlow"
# }

curl --insecure -X POST https://localhost:5001/api/openbooking/test-interface/datasets/uat-ci/opportunities \
  -H "Content-Type: application/vnd.openactive.booking+json; version=1" \
  -H "X-OpenActive-Test-Client-Id: booking-partner-1" \
  -H "X-OpenActive-Test-Seller-Id: https://localhost:5001/api/identifiers/seller" \
  -d '{
    "@type": "ScheduledSession",
    "superEvent": {
      "@type": "SessionSeries",
      "organizer": {
        "@type": "Organization",
        "@id": "https://localhost:5001/api/identifiers/seller"
      }
    },
    "@context": [
      "https://openactive.io/",
      "https://openactive.io/test-interface"
    ],
    "test:testOpportunityCriteria": "https://openactive.io/test-interface#TestOpportunityBookableInPast",
    "test:testOpenBookingFlow": "https://openactive.io/test-interface#OpenBookingSimpleFlow"
  }'
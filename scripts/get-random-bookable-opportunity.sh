curl -X POST --location 'http://localhost:3000/test-interface/datasets/uat-ci/opportunities' \
  --header 'Content-Type: application/json' \
  --data-raw '{
    "@context": [
      "https://openactive.io/",
      "https://openactive.io/test-interface"
    ],
    "@type": "ScheduledSession",
    "superEvent": {
      "@type": "SessionSeries",
      "organizer": {
        "@type": "Organization",
        "@id": "https://localhost:5001/api/identifiers/seller"
      }
    },
    "test:testOpenBookingFlow": "https://openactive.io/test-interface#OpenBookingSimpleFlow",
    "test:testOpportunityCriteria": "https://openactive.io/test-interface#TestOpportunityBookable"
  }'
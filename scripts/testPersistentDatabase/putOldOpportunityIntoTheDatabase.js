/*

TODO3 the plan is:

1. PUT OLD OPP INTO DB
  a. put old opp into ref impl db
  b. use broker to check that there are no bookable opps seen (HTTP 404)
2. CHECK DATA REFRESHER (separate script or entrypoint into the same file)
  a. wait for data refresher to run
  b. use broker to check that there IS A bookable opp in feed
*/

/**
 * Sends a POST request to create an old opportunity in the database
 * @returns {Promise<Response>} The response from the API
 */
async function putOldOpportunityIntoRefImplDb() {
  const payload = {
    '@type': 'ScheduledSession',
    'superEvent': {
      '@type': 'SessionSeries',
      'organizer': {
        '@type': 'Organization',
        '@id': 'https://localhost:5001/api/identifiers/seller'
      }
    },
    '@context': [
      'https://openactive.io/',
      'https://openactive.io/test-interface'
    ],
    'test:testOpportunityCriteria': 'https://openactive.io/test-interface#TestOpportunityBookableInPast',
    'test:testOpenBookingFlow': 'https://openactive.io/test-interface#OpenBookingSimpleFlow'
  };

  const res = await fetch('https://localhost:5001/api/openbooking/test-interface/datasets/uat-ci/opportunities', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/vnd.openactive.booking+json; version=1',
      'X-OpenActive-Test-Client-Id': 'booking-partner-1',
      'X-OpenActive-Test-Seller-Id': 'https://localhost:5001/api/identifiers/seller'
    },
    body: JSON.stringify(payload),
    // Since the original curl uses --insecure
    agent: new (require('https').Agent)({
      rejectUnauthorized: false
    })
  });
  const data = await res.json();
  return data;
}

async function getRandomBookableOpportunityFromBroker() {
  const payload = {
    '@context': [
      'https://openactive.io/',
      'https://openactive.io/test-interface'
    ],
    '@type': 'ScheduledSession',
    'superEvent': {
      '@type': 'SessionSeries',
      'organizer': {
        '@type': 'Organization',
        '@id': 'https://localhost:5001/api/identifiers/seller'
      }
    },
    'test:testOpportunityCriteria': 'https://openactive.io/test-interface#TestOpportunityBookable',
    'test:testOpenBookingFlow': 'https://openactive.io/test-interface#OpenBookingSimpleFlow'
  };

  const res = await fetch('http://localhost:3000/test-interface/datasets/uat-ci/opportunities', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(payload),
  });
  const data = await res.json();
  console.log('status:', res.status);
  console.log('data:', data);
  return data;
}

getRandomBookableOpportunityFromBroker();
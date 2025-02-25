const http = require('http');

// This is required for localhost RefImpl, which requires https with a
// self-signed certificate
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

/*

TODO2 the test plan is:

STEP 1:

a. Turn on RefImpl like
  ```
  ASPNETCORE_ENVIRONMENT=single-seller \
  OPPORTUNITY_COUNT=0 \
  SQLITE_DB_PATH=/Users/lukewinship/Databases/openactive-fakedatabase.db \
  dotnet run --no-launch-profile \
    --project ./Examples/BookingSystem.AspNetCore/BookingSystem.AspNetCore.csproj \
    --configuration Release
  ```
b. Run `node scripts/testPersistentDatabase.js 1a` (insert old opp)
c. Turn on Broker
d. Run `node scripts/testPersistentDatabase.js 1d` (assert no bookable opp)
e. Turn off Broker, RefImpl.

STEP 2:

a. Turn on RefImpl like
  ```
  ASPNETCORE_ENVIRONMENT=single-seller \
  OPPORTUNITY_COUNT=0 \
  SQLITE_DB_PATH=/Users/lukewinship/Databases/openactive-fakedatabase.db \
  PERIODICALLY_REFRESH_DATA=true \
  PERSIST_PREVIOUS_DATABASE=true \
  dotnet run --no-launch-profile \
    --project ./Examples/BookingSystem.AspNetCore/BookingSystem.AspNetCore.csproj \
    --configuration Release
  ```
b. Wait for data refresher to run
  - Wait 10s? (TODO there must be a better way)
c. Turn on Broker
d. Run `node scripts/testPersistentDatabase.js 2d` (assert bookable opp)
e. Turn off RefImpl

TODO (for a later issue): test that the refreshed opportunity has modifieds updated as expected
*/

if (require.main === module) {
  const step = process.argv[2];

  if (step === '1a') {
    testStep1aInsertOldOpportunity();
  } else if (step === '1d') {
    testStep1dAssertNoBookableOpportunity(); 
  } else if (step === '2d') {
    testStep2dAssertBookableOpportunity();
  } else {
    console.error('Please provide step argument: 1a or 1d or 2d');
    process.exit(1);
  }

}

async function testStep1aInsertOldOpportunity() {
  const putRes = await putOldOpportunityIntoRefImplDb();
  if (putRes.status < 200 || putRes.status >= 300) {
    console.error('Failed to put old opportunity into ref impl db:', putRes.status, putRes.data);
    process.exit(1);
  }
  console.log('Put old opportunity into ref impl db:', putRes.status, putRes.data);
}

// TODO testStep1b should be: starting broker

async function testStep1dAssertNoBookableOpportunity() {
  const getRes = await getRandomBookableOpportunityFromBroker();
  if (getRes.status !== 404) {
    console.error('Got random bookable opportunity from broker, but there should be none, as the inserted opportunity is in the past',
      getRes.status, getRes.data);
    process.exit(1);
  }
  console.log('Asserted that there is no bookable opportunity in broker feed');
}

async function testStep2dAssertBookableOpportunity() {
  const getRes = await getRandomBookableOpportunityFromBroker();
  if (getRes.status < 200 || getRes.status >= 300) {
    console.error('Could not find random bookable opportunity from broker, but there should be one, as the previously-past opportunity should have been refreshed into the future',
      getRes.status, getRes.data);
    process.exit(1);
  }
  console.log('Asserted that there is a bookable opportunity in broker feed');
}
/**
 * Sends a POST request to create an old opportunity in the database
 * @returns {Promise<any>} The response from the API
 */
async function putOldOpportunityIntoRefImplDb() {
  const payload = {
    ...getPayloadOpportunityDataForPutOldOpportunity('IndividualFacilityUseSlot'),
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
    agent: new http.Agent({
      rejectUnauthorized: false
    })
  });
  const data = await res.json();
  return { status: res.status, data };
}

/**
 * @param {'ScheduledSession' | 'IndividualFacilityUseSlot' | 'FacilityUseSlot'} opportunityType
 */
function getPayloadOpportunityDataForPutOldOpportunity(opportunityType) {
  const seller = {
    '@type': 'Organization',
    '@id': 'https://localhost:5001/api/identifiers/seller'
  };
  switch (opportunityType) {
    case 'ScheduledSession':
      return {
        '@type': 'ScheduledSession',
        'superEvent': {
          '@type': 'SessionSeries',
          'organizer': seller,
        },
      };
    case 'IndividualFacilityUseSlot':
      return {
        '@type': 'Slot',
        facilityUse: {
          '@type': 'IndividualFacilityUse',
          provider: seller,
        },
      };
    case 'FacilityUseSlot':
      return {
        '@type': 'Slot',
        facilityUse: {
          '@type': 'FacilityUse',
          provider: seller,
        },
      };
    default:
      throw new Error(`Invalid opportunity type: ${opportunityType}`);
  }
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
  return { status: res.status, data };
}
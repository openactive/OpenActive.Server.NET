/**
 * This script tests the persistence and data refreshing capabilities of the
 * OpenActive FakeDatabase.
 * 
 * It simulates a complete lifecycle of the database by:
 *
 * 1. Creating a fresh database with a past (and so, non-bookable) opportunity
 * 2. Verifying the broker correctly identifies it as non-bookable
 * 3. Shutting down and restarting the services
 * 4. Waiting for the data refresher to create new opportunities
 * 5. Verifying that new bookable opportunities appear in the feed
 * 
 * The script is structured as a Node.js program rather than directly in GitHub
 * Actions to simplify process management and provide better control over the
 * test sequence.
 *
 * TODOs (potentially for later issues):
 * - Test that the refreshed opportunity has modifieds updated as expected
 */
const http = require('http');

// This is required for localhost RefImpl, which requires https with a
// self-signed certificate
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

if (require.main === module) {
  start();
}

/* TODO use ample logging to make it clear what step we have executed so far. As
this logic is being orchestrated in a node.js script, rather than directly in
Github action (where process management would be more complicated), it'll be
harder to see what's going on without good logging. */
async function start() {
  // Step 1: Create a fresh database, and put an past opportunity into it
  // Step 1a: Turn on RefImpl, configured to create a fresh database
  /*
  TODO spawn a process to run RefImpl in the background, in such a way that it can be killed later.
  Run with these env vars:

  ASPNETCORE_ENVIRONMENT: single-seller
  OPPORTUNITY_COUNT: 0
  SQLITE_DB_PATH: ~/openactive-fakedatabase.db
  */

  // Step 1b: Put an past opportunity into the database
  await testStep1bInsertOldOpportunity('ScheduledSession', 'https://openactive.io/test-interface#TestOpportunityBookableInPast');

  // Step 1c: Turn on Broker
  /*
  TODO spawn a process to run Broker in the background, in such a way that it can be killed later.
  Run with these env vars:
 
  ASPNETCORE_ENVIRONMENT: single-seller
  FORCE_COLOR: 1
  NODE_CONFIG: |
    {"broker": {"outputPath": "../../output/"}}
  NODE_ENV: .example.single-seller
  NODE_APP_INSTANCE: ci
  */

  // Step 1d: Assert that there is no bookable opportunity in the broker feed
  await testStep1dAssertNoBookableOpportunity();

  // Step 1e: Turn off Broker, RefImpl
  // TODO kill those processes

  // Step 2: Using the same database, await data refresher and find the past
  // opportunity turned into a future opportunity

  // Step 2a: Turn on RefImpl
  /*
  TODO spawn a process to run RefImpl in the background, in such a way that it can be killed later.
  Run with these env vars:

  ASPNETCORE_ENVIRONMENT=single-seller
  OPPORTUNITY_COUNT=0
  SQLITE_DB_PATH: ~/openactive-fakedatabase.db
  PERIODICALLY_REFRESH_DATA=true
  PERSIST_PREVIOUS_DATABASE=true
  */

  // Step 2b: Wait for Data Refresher to have completed one cycle
  await testStep2bAwaitDataRefresher();

  // Step 2c: Turn on Broker
  // TODO: same as 1c

  // Step 2d: Assert that there is now a bookable opportunity in the broker feed
  await testStep2dAssertBookableOpportunity();

  // Step 2e: Turn off Broker, RefImpl
  // TODO kill those processes and exit
}

/**
 * @param {'ScheduledSession' | 'IndividualFacilityUseSlot' | 'FacilityUseSlot'} opportunityType
 * @param {string} criteria
 */
async function testStep1bInsertOldOpportunity(opportunityType, criteria) {
  const putRes = await putOldOpportunityIntoRefImplDb(opportunityType, criteria);
  if (!isHttpStatusSuccess(putRes.status)) {
    console.error('Failed to put old opportunity into ref impl db:', putRes.status, putRes.data);
    process.exit(1);
  }
  console.log('Put old opportunity into ref impl db:', putRes.status, putRes.data);
}

async function testStep1dAssertNoBookableOpportunity() {
  // Await Broker health check first, to ensure that it has processed all feeds.
  await awaitBrokerHealthCheck();

  {
    const getRes = await getRandomBookableOpportunityFromBroker(
      'ScheduledSession',
      'https://openactive.io/test-interface#TestOpportunityBookableInPast',
    );
    if (!isHttpStatusSuccess(getRes.status)) {
      console.error('Could not find random bookable past-opportunity from broker, but there should be one, as it was created in step 1a');
      process.exit(1);
    }
    console.log('Confirmed that past-bookable opportunity was found in broker feed');
  }
  
  {
    const getRes = await getRandomBookableOpportunityFromBroker(
      'ScheduledSession',
      'https://openactive.io/test-interface#TestOpportunityBookable',
    );
    if (isHttpStatusSuccess(getRes.status)) {
      console.error('Got random bookable (future-)opportunity from broker, but there should be none, as the inserted opportunity is in the past',
        getRes.status, getRes.data);
      process.exit(1);
    }
    console.log('Asserted that there is no bookable opportunity in broker feed');
  }
}

async function testStep2bAwaitDataRefresher() {
  const res = await fetch('https://localhost:5001/init-wait/data-refresher', {
    method: 'GET',
    agent: new http.Agent({
      rejectUnauthorized: false
    })
  });
  if (!isHttpStatusSuccess(res.status)) {
    console.error('There was an error waiting for the data refresher to complete its first cycle', res.status, res.data);
    process.exit(1);
  }
  console.log('Data refresher has completed its first cycle');
}

async function testStep2dAssertBookableOpportunity() {
  // Await Broker health check first, to ensure that it has processed all feeds.
  await awaitBrokerHealthCheck();

  const getRes = await getRandomBookableOpportunityFromBroker();
  if (!isHttpStatusSuccess(getRes.status)) {
    console.error('Could not find random bookable opportunity from broker, but there should be one, as the previously-past opportunity should have been refreshed into the future',
      getRes.status, getRes.data);
    process.exit(1);
  }
  console.log('Asserted that there is a bookable opportunity in broker feed');
}
/**
 * Sends a POST request to create an old opportunity in the database
 * @param {'ScheduledSession' | 'IndividualFacilityUseSlot' | 'FacilityUseSlot'} opportunityType
 * @param {string} criteria
 */
async function putOldOpportunityIntoRefImplDb(opportunityType, criteria) {
  const payload = {
    ...getOpportunityPartForTestInterfaceCreateOpportunityOpportunityPayload(opportunityType),
    '@context': [
      'https://openactive.io/',
      'https://openactive.io/test-interface'
    ],
    'test:testOpportunityCriteria': criteria,
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
function getOpportunityPartForTestInterfaceCreateOpportunityOpportunityPayload(opportunityType) {
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

/**
 * @param {'ScheduledSession' | 'IndividualFacilityUseSlot' | 'FacilityUseSlot'} opportunityType
 * @param {string} criteria
 */
async function getRandomBookableOpportunityFromBroker(opportunityType, criteria) {
  const payload = {
    ...getOpportunityPartForTestInterfaceCreateOpportunityOpportunityPayload(opportunityType),
    '@context': [
      'https://openactive.io/',
      'https://openactive.io/test-interface'
    ],
    'test:testOpportunityCriteria': criteria,
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

async function awaitBrokerHealthCheck() {
  console.log('Awaiting Broker health check...');
  const res = await fetch('http://localhost:3000/health-check');
  if (!isHttpStatusSuccess(res.status)) {
    throw new Error('Broker health check failed');
  }
  console.log('Broker health check passed');
  return true;
}

/**
 * @param {number} status
 * @returns {boolean}
 */
function isHttpStatusSuccess(status) {
  return status >= 200 && status < 300;
}

/**
 * Helper to manage unique Voter Token stored in localStorage.
 * Ensures each browser instance has a unique identity for voting deduplication.
 */
export function getVoterToken() {
  let token = localStorage.getItem('poll_voter_token');
  if (!token) {
    const randomPart = Math.random().toString(36).substring(2, 10);
    const timePart = Date.now().toString(36);
    token = `voter_${randomPart}_${timePart}`;
    localStorage.setItem('poll_voter_token', token);
  }
  return token;
}

export function hasVotedLocally(pollCode) {
  return localStorage.getItem(`voted_${pollCode}`) === 'true';
}

export function markVotedLocally(pollCode) {
  localStorage.setItem(`voted_${pollCode}`, 'true');
}

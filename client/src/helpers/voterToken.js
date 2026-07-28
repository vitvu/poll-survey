export function getVoterToken() {
  let token = localStorage.getItem('poll_voter_token');
  if (!token) {
    token = `voter_${Math.random().toString(36).substring(2, 10)}_${Date.now().toString(36)}`;
    localStorage.setItem('poll_voter_token', token);
  }
  return token;
}

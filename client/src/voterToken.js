// generate unique voter token stored in localstorage
// used to prevent duplicate votes without requiring login
export function getVoterToken() {
  let token = localStorage.getItem('poll_voter_token')

  if (token === null) {
    let randomPart = ''
    for (let i = 0; i < 8; i++) {
      randomPart += Math.floor(Math.random() * 10)
    }
    token = 'voter_' + randomPart
    localStorage.setItem('poll_voter_token', token)
  }

  return token
}

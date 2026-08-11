
export function getVoterToken() {
  let token = localStorage.getItem('poll_voter_token')

  if (token === null) {
    let randomPart = ''
    for (let i = 0; i < 8; i++) {
      const digit = Math.floor(Math.random() * 10)
      randomPart += digit
    }
    token = 'voter_' + randomPart
    localStorage.setItem('poll_voter_token', token)
  }

  return token
}

# Test cases (member 5)
- One client: echo ID and payload correct.
- 10 and 50 concurrent clients: separate connections, matching echo.
- Disconnect halfway through frame; new valid client succeeds.
- Send invalid length / oversized length; new valid client succeeds.
- Idle or partial message timeout; new valid client succeeds.
Record expected/actual/pass and evidence. Run only against local project server.

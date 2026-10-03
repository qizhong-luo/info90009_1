// Test-only provider fixture. No OpenAI credentials or external network calls.
import { createServer } from '../server.mjs';
const fetchImpl = async (_url, init) => {
  const payload = JSON.parse(init.body);
  const result = payload.input.find(i => i.type === 'function_call_output');
  let output;
  if (!result && typeof payload.tool_choice === 'object') output = [{ type: 'function_call',
    name: payload.tool_choice.name, arguments: '{}', call_id: 'fixture_call' }];
  else {
    const data = result ? JSON.parse(result.output) : null;
    const summary = data?.appSessionCount !== undefined
      ? `${data.appSessionCount} app sessions, ${data.appSessionMinutes} minutes of app use (not measured sleep). ${data.sampleCount} sample records excluded.`
      : data?.activities ? data.activities.join(', ') : 'Conversation received.';
    output = [{ type: 'message', content: [{ type: 'output_text',
      text: 'Fixture verified (local test, not a real OpenAI reply): ' + summary }] }];
  }
  return { ok: true, json: async () => ({ status: 'completed', output }) };
};
const server = createServer({ apiKey: 'fixture', model: 'fixture', token: 'fixture-token', fetchImpl });
server.listen(8788, '127.0.0.1', () => console.log('Unity integration fixture ready on 127.0.0.1:8788 (no external API).'));

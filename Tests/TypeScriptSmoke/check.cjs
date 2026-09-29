const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
let ts;
try { ts = require(process.argv[2] || 'typescript'); }
catch { throw new Error('TypeScript compiler not found. Run npm install in Tests/TypeScriptSmoke, or pass a path to typescript.js.'); }
const generated = path.resolve(__dirname, '../TypeScriptGenerated');
function files(dir) {
  return fs.readdirSync(dir, { withFileTypes: true }).flatMap(e => {
    const full = path.join(dir, e.name);
    return e.isDirectory() ? (e.name === 'runtime' ? [] : files(full)) : (e.name.endsWith('.ts') ? [full] : []);
  });
}
const sources = [...files(generated), path.join(__dirname, 'types.ts')];
const options = {
  target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext,
  moduleResolution: ts.ModuleResolutionKind.Bundler, strict: true,
  exactOptionalPropertyTypes: true, noUncheckedIndexedAccess: true,
  isolatedModules: true, verbatimModuleSyntax: true, useDefineForClassFields: true,
  noEmit: true, skipLibCheck: false
};
const program = ts.createProgram(sources, options);
const errors = ts.getPreEmitDiagnostics(program);
if (errors.length) {
  console.error(ts.formatDiagnosticsWithColorAndContext(errors, {
    getCurrentDirectory: () => __dirname, getCanonicalFileName: f => f, getNewLine: () => '\n'
  }));
  process.exit(1);
}
// Transpile the actual generated modules to check that type-only navigation imports cause no runtime cycles.
const main = path.join(generated, 'Main');
const runtime = path.join(generated, 'runtime');
for (const file of files(main)) {
  const target = path.join(runtime, path.relative(main, file)).replace(/\.ts$/, '.js');
  const result = ts.transpileModule(fs.readFileSync(file, 'utf8'), {
    compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.CommonJS }
  });
  fs.mkdirSync(path.dirname(target), { recursive: true });
  fs.writeFileSync(target, result.outputText);
}
const models = require(path.join(runtime, 'index.js'));
const row = new models.WireTypes();
assert.deepEqual(Object.keys(row), [], 'Declarations must not invent API values');
Object.assign(row, JSON.parse('{"id":1,"active":true,"optionalName":null,"createdAt":"2026-09-29T12:00:00Z","blob":"AQI=","constructor":7,"urlValue":"test"}'));
assert.equal(row.createdAt, '2026-09-29T12:00:00Z');
assert.equal(row.blob, 'AQI=');
assert.equal(row.optionalName, null);
assert.equal(row.constructor, 7);
assert.ok(row instanceof models.WireTypes);
assert.equal(new models.Customer().manager, undefined, 'Self-reference must not allocate recursively');
assert.equal(models.UnknownResult, undefined);
const parameters = new models.DefaultsParameters();
assert.equal(JSON.stringify(parameters), '{}');
parameters.first = null;
parameters.last = 0;
assert.equal(JSON.stringify(parameters), '{"first":null,"last":0}', 'JSON must preserve explicit null/zero and omit missing inputs');
const inputOutput = Object.assign(new models.DoWorkParameters(), { class: 'text', cancellationToken: null, p0: null });
assert.equal(JSON.stringify(inputOutput), '{"class":"text","cancellationToken":null,"p0":null}');
assert.equal(models.BrokenParametersParameters, undefined, 'Incomplete input schema must not create a DTO');
assert.equal(typeof models.UnknownParameters, 'function', 'Known inputs remain available when result shape is unknown');
console.log(`PASS: TypeScript ${ts.version}; ${sources.length} files checked in strict/isolatedModules mode; JSON contracts and runtime classes verified.`);

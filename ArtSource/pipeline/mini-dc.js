/**
 * Minimal renderer for the design project's .dc.html components — replaces the
 * proprietary support.js runtime with just enough semantics to render sprite
 * variants for extraction:
 *   {{ expr }} interpolation · sc-if · sc-for · dc-import (recursive)
 * Components' own <script type="text/x-dc"> logic executes verbatim (their
 * class Component extends DCLogic { renderVals() }), so all part geometry and
 * palettes come from the design's own data, not a port.
 */
/* eslint-disable no-eval */
class DCLogic {
  constructor(props) { this.props = props || {}; }
  setState() {} // components under extraction never animate state
}

const componentCache = new Map();

async function fetchComponent(name) {
  if (componentCache.has(name)) return componentCache.get(name);
  const res = await fetch(`./${name}.dc.html`);
  if (!res.ok) throw new Error(`component ${name}: HTTP ${res.status}`);
  const text = await res.text();
  const doc = new DOMParser().parseFromString(text, 'text/html');
  const root = doc.querySelector('x-dc');
  const script = doc.querySelector('script[data-dc-script]');
  const helmet = root.querySelector('helmet');
  const entry = {
    template: root,
    helmetHtml: helmet ? helmet.innerHTML : '',
    code: script ? script.textContent : 'class Component extends DCLogic { renderVals() { return {}; } }',
  };
  componentCache.set(name, entry);
  return entry;
}

function evalExpr(expr, scope) {
  const names = Object.keys(scope);
  const vals = names.map(k => scope[k]);
  try {
    return Function(...names, `return (${expr});`)(...vals);
  } catch (e) {
    throw new Error(`expr "${expr}": ${e.message}`);
  }
}

function interpolate(text, scope) {
  return text.replace(/\{\{\s*([\s\S]+?)\s*\}\}/g, (_, expr) => {
    const v = evalExpr(expr, scope);
    return v == null ? '' : String(v);
  });
}

function attrValue(raw, scope) {
  const m = raw.match(/^\{\{\s*([\s\S]+?)\s*\}\}$/);
  if (m) return evalExpr(m[1], scope); // sole-expression attr keeps its type
  return interpolate(raw, scope);
}

async function renderNode(node, scope, out) {
  if (node.nodeType === Node.TEXT_NODE) {
    out.appendChild(document.createTextNode(interpolate(node.textContent, scope)));
    return;
  }
  if (node.nodeType !== Node.ELEMENT_NODE) return;
  const tag = node.tagName.toLowerCase();

  if (tag === 'helmet' || tag === 'script') return;

  if (tag === 'sc-if') {
    if (evalExpr(node.getAttribute('value').replace(/^\{\{|\}\}$/g, ''), scope)) {
      for (const child of node.childNodes) await renderNode(child, scope, out);
    }
    return;
  }

  if (tag === 'sc-for') {
    const list = attrValue(node.getAttribute('list'), scope) || [];
    const alias = node.getAttribute('as');
    for (const item of list) {
      const inner = Object.assign({}, scope, { [alias]: item });
      for (const child of node.childNodes) await renderNode(child, inner, out);
    }
    return;
  }

  if (tag === 'dc-import') {
    const name = node.getAttribute('name');
    const props = {};
    for (const attr of node.attributes) {
      if (attr.name === 'name' || attr.name.startsWith('hint-')) continue;
      if (attr.name === 'style') continue; // host styling applied to wrapper below
      props[attr.name] = attrValue(attr.value, scope);
    }
    const wrapper = document.createElement('div');
    if (node.getAttribute('style'))
      wrapper.setAttribute('style', interpolate(node.getAttribute('style'), scope));
    out.appendChild(wrapper);
    await instantiate(name, props, wrapper);
    return;
  }

  const el = document.createElement(tag);
  for (const attr of node.attributes) {
    if (attr.name.startsWith('hint-') || attr.name.startsWith('on')) continue;
    el.setAttribute(attr.name, interpolate(attr.value, scope));
  }
  out.appendChild(el);
  for (const child of node.childNodes) await renderNode(child, scope, el);
}

async function instantiate(name, props, mount) {
  const { template, helmetHtml, code } = await fetchComponent(name);
  if (helmetHtml && !document.getElementById(`helmet-${name}`)) {
    const h = document.createElement('div');
    h.id = `helmet-${name}`;
    h.innerHTML = helmetHtml; // keyframes/styles the parts reference
    document.head.appendChild(h);
  }
  const Component = Function('DCLogic', `${code}; return Component;`)(DCLogic);
  const comp = new Component(props);
  comp.props = props;
  const vals = comp.renderVals ? comp.renderVals() : {};
  for (const child of template.childNodes) await renderNode(child, vals, mount);
}

/** Entry: ?component=LaneObject&w=186&h=48&props=<json-uri> */
window.renderTarget = async function () {
  const q = new URLSearchParams(location.search);
  const name = q.get('component');
  const props = JSON.parse(decodeURIComponent(q.get('props') || '{}'));
  const w = q.get('w'), h = q.get('h');
  const mount = document.getElementById('mount');
  mount.style.width = `${w}px`;
  mount.style.height = `${h}px`;
  await instantiate(name, props, mount);
  // freeze all animations at their first keyframe unless a frame offset is given
  const offset = parseFloat(q.get('animOffset') || '0');
  for (const el of document.querySelectorAll('*')) {
    const cs = getComputedStyle(el);
    if (cs.animationName !== 'none') {
      el.style.animationPlayState = 'paused';
      el.style.animationDelay = `${-offset}s`;
    }
  }
  document.title = 'READY';
};

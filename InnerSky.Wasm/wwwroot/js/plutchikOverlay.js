// plutchikOverlay.js
// Ported from the standalone preview.html. The SVG calibration (coordinates, sizes,
// rotations) is preserved verbatim. The ONLY behavioral changes vs. the original:
//   * alert(label) is replaced by callbacks into .NET
//   * tapping a bare base circle commits nothing (it just expands the 3 intensity
//     options); the commit happens when an intensity circle is tapped
//   * dyad / intensity taps notify the EmotionStateService via the .NET ref
//
// Public API:  init(svgEl, stageEl, toggleEl, dotNetRef) -> { dispose }

const baseEmotions = [
  { id: "joy",          label: "Joy",          cx: 699,  cy: 252,  rx: 90, ry: 90, rot: 0 },
  { id: "trust",        label: "Trust",        cx: 1017, cy: 384,  rx: 90, ry: 90, rot: 0 },
  { id: "fear",         label: "Fear",         cx: 1149, cy: 702,  rx: 90, ry: 90, rot: 0 },
  { id: "surprise",     label: "Surprise",     cx: 1017, cy: 1020, rx: 90, ry: 90, rot: 0 },
  { id: "sadness",      label: "Sadness",      cx: 699,  cy: 1152, rx: 90, ry: 90, rot: 0 },
  { id: "disgust",      label: "Disgust",      cx: 381,  cy: 1020, rx: 90, ry: 90, rot: 0 },
  { id: "anger",        label: "Anger",        cx: 249,  cy: 702,  rx: 90, ry: 90, rot: 0 },
  { id: "anticipation", label: "Anticipation", cx: 381,  cy: 384,  rx: 90, ry: 90, rot: 0 }
];

const dyads = [
  { id: "optimism",       label: "Optimism",       type: "primary",   cx: 529,  cy: 289,  rx: 78,  ry: 25, rot: -18 },
  { id: "love",           label: "Love",           type: "primary",   cx: 869,  cy: 289,  rx: 62,  ry: 24, rot: 18 },
  { id: "submission",     label: "Submission",     type: "primary",   cx: 1115, cy: 531,  rx: 78,  ry: 24, rot: 66 },
  { id: "awe",            label: "Awe",            type: "primary",   cx: 1122, cy: 866,  rx: 55,  ry: 22, rot: -66 },
  { id: "disapproval",    label: "Disapproval",    type: "primary",   cx: 892,  cy: 1108, rx: 100, ry: 26, rot: -18 },
  { id: "remorse",        label: "Remorse",        type: "primary",   cx: 520,  cy: 1107, rx: 84,  ry: 26, rot: 18 },
  { id: "contempt",       label: "Contempt",       type: "primary",   cx: 285,  cy: 871,  rx: 72,  ry: 23, rot: 66 },
  { id: "aggressiveness", label: "Aggressiveness", type: "primary",   cx: 292,  cy: 525,  rx: 89,  ry: 25, rot: -66 },

  { id: "hope",           label: "Hope",           type: "secondary", cx: 699,  cy: 95,   rx: 65,  ry: 30, rot: 0 },
  { id: "guilt",          label: "Guilt",          type: "secondary", cx: 1137, cy: 290,  rx: 50,  ry: 24, rot: 45 },
  { id: "curiosity",      label: "Curiosity",      type: "secondary", cx: 1310, cy: 701,  rx: 82,  ry: 30, rot: 90 },
  { id: "despair",        label: "Despair",        type: "secondary", cx: 1142, cy: 1110, rx: 80,  ry: 34, rot: -45 },
  { id: "unbelief",       label: "Unbelief",       type: "secondary", cx: 699,  cy: 1296, rx: 82,  ry: 30, rot: 0 },
  { id: "envy",           label: "Envy",           type: "secondary", cx: 260,  cy: 1109, rx: 72,  ry: 34, rot: 45 },
  { id: "cynicism",       label: "Cynicism",       type: "secondary", cx: 93,   cy: 701,  rx: 82,  ry: 30, rot: 90 },
  { id: "pride",          label: "Pride",          type: "secondary", cx: 266,  cy: 277,  rx: 62,  ry: 30, rot: -45 },

  { id: "dominance",      label: "Dominance",      type: "tertiary",  cx: 578,  cy: 425,  rx: 96,  ry: 25, rot: -18 },
  { id: "anxiety",        label: "Anxiety",        type: "tertiary",  cx: 800,  cy: 423,  rx: 72,  ry: 28, rot: 18 },
  { id: "delight",        label: "Delight",        type: "tertiary",  cx: 977,  cy: 577,  rx: 70,  ry: 25, rot: 72 },
  { id: "sentimentality", label: "Sentimentality", type: "tertiary",  cx: 987,  cy: 830,  rx: 112, ry: 25, rot: -66 },
  { id: "shame",          label: "Shame",          type: "tertiary",  cx: 818,  cy: 998,  rx: 70,  ry: 25, rot: -18 },
  { id: "outrage",        label: "Outrage",        type: "tertiary",  cx: 596,  cy: 988,  rx: 75,  ry: 25, rot: 18 },
  { id: "pessimism",      label: "Pessimism",      type: "tertiary",  cx: 427,  cy: 825,  rx: 88,  ry: 25, rot: 74 },
  { id: "morbidness",     label: "Morbidness",     type: "tertiary",  cx: 435,  cy: 576,  rx: 92,  ry: 25, rot: -72 }
];

// Visual labels for the expanded intensity circles. (Naming for the model lives in
// C# PlutchikData; this copy is purely for what the circles display.)
const intensityLevels = {
  joy:          { mild: 'Serenity',     base: 'Joy',          intense: 'Ecstasy' },
  trust:        { mild: 'Acceptance',   base: 'Trust',        intense: 'Admiration' },
  fear:         { mild: 'Apprehension', base: 'Fear',         intense: 'Terror' },
  surprise:     { mild: 'Distraction',  base: 'Surprise',     intense: 'Amazement' },
  sadness:      { mild: 'Pensiveness',  base: 'Sadness',      intense: 'Grief' },
  disgust:      { mild: 'Boredom',      base: 'Disgust',      intense: 'Loathing' },
  anger:        { mild: 'Annoyance',    base: 'Anger',        intense: 'Rage' },
  anticipation: { mild: 'Interest',     base: 'Anticipation', intense: 'Vigilance' }
};

function createSvgElement(tag, attrs = {}, classNames = []) {
  const el = document.createElementNS('http://www.w3.org/2000/svg', tag);
  Object.entries(attrs).forEach(([k, v]) => el.setAttribute(k, v));
  classNames.forEach(n => el.classList.add(n));
  return el;
}

export function init(overlay, stage, toggleRegions, dotNetRef) {
  let activeBaseId = null;
  let intensityLayer = null;

  function clearIntensityLayer() {
    if (intensityLayer) {
      intensityLayer.remove();
      intensityLayer = null;
    }
    activeBaseId = null;
  }

  function positionIntensityCircles(base) {
    const offset = 128;
    const rx = 76;
    const ry = 48;
    const baseDrop = ry; // center of base circle near the bottom edge of mild/intense
    return {
      mild:    { cx: base.cx - offset, cy: base.cy,            rx, ry },
      base:    { cx: base.cx,          cy: base.cy + baseDrop, rx, ry },
      intense: { cx: base.cx + offset, cy: base.cy,            rx, ry }
    };
  }

  function renderIntensityNode(group, node, label, kind, base) {
    const circle = createSvgElement('ellipse', {
      cx: node.cx, cy: node.cy, rx: node.rx, ry: node.ry,
      tabindex: '0', role: 'button',
      'aria-label': `${label} (${kind} ${base.label})`,
      'data-name': label, 'data-kind': kind
    }, ['intensity-circle', kind]);

    const tag = createSvgElement('text', { x: node.cx, y: node.cy - 15 }, ['intensity-tag']);
    tag.textContent = kind;

    const text = createSvgElement('text', { x: node.cx, y: node.cy + 8 }, ['intensity-label']);
    text.textContent = label;

    function handleActivate(event) {
      event.stopPropagation();
      // Commit: this single emotion at the chosen level. C# resolves the name.
      dotNetRef.invokeMethodAsync('OnIntensitySelected', base.id, kind);
    }

    circle.addEventListener('click', handleActivate);
    circle.addEventListener('keydown', e => {
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); handleActivate(e); }
    });

    group.appendChild(circle);
    group.appendChild(tag);
    group.appendChild(text);
  }

  function expandBaseEmotion(base) {
    if (activeBaseId === base.id) { clearIntensityLayer(); return; }
    clearIntensityLayer();
    activeBaseId = base.id;

    const levels = intensityLevels[base.id];
    if (!levels) return;

    const nodes = positionIntensityCircles(base);
    intensityLayer = createSvgElement('g',
      { id: `intensity-layer-${base.id}`, 'data-base': base.id }, ['intensity-layer']);

    intensityLayer.appendChild(createSvgElement('line', {
      x1: nodes.mild.cx, y1: nodes.mild.cy, x2: nodes.intense.cx, y2: nodes.intense.cy
    }, ['intensity-connector']));

    renderIntensityNode(intensityLayer, nodes.mild,    levels.mild,    'mild',    base);
    renderIntensityNode(intensityLayer, nodes.base,    levels.base,    'base',    base);
    renderIntensityNode(intensityLayer, nodes.intense, levels.intense, 'intense', base);
    overlay.appendChild(intensityLayer);
  }

  function makeEllipse(item, kind) {
    const ellipse = createSvgElement('ellipse', {
      id: `hit-${item.id}`, cx: item.cx, cy: item.cy, rx: item.rx, ry: item.ry,
      tabindex: '0', role: 'button', 'aria-label': item.label,
      'data-id': item.id, 'data-name': item.label, 'data-kind': kind
    }, ['tap-zone']);

    if (item.type) ellipse.dataset.type = item.type;
    if (kind === 'base') ellipse.classList.add('base-emotion');
    else ellipse.classList.add(`dyad-${item.type}`);
    if (item.rot) ellipse.setAttribute('transform', `rotate(${item.rot} ${item.cx} ${item.cy})`);

    function handleActivate(event) {
      event.stopPropagation();
      if (kind === 'base') {
        // Bare base circle: expand the three options only. Commits nothing.
        expandBaseEmotion(item);
      } else {
        // Dyad: commit immediately (both emotions at Base). C# resets + renames.
        clearIntensityLayer();
        dotNetRef.invokeMethodAsync('OnDyadSelected', item.id);
      }
    }

    ellipse.addEventListener('click', handleActivate);
    ellipse.addEventListener('keydown', e => {
      if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); handleActivate(e); }
    });
    return ellipse;
  }

  // Build the overlay (dyads first, then base circles on top — matching the original).
  dyads.forEach(item => overlay.appendChild(makeEllipse(item, 'dyad')));
  baseEmotions.forEach(item => overlay.appendChild(makeEllipse(item, 'base')));

  const collapse = () => clearIntensityLayer();
  overlay.addEventListener('click', collapse);

  const onToggle = () => stage.classList.toggle('show-regions', toggleRegions.checked);
  if (toggleRegions) toggleRegions.addEventListener('change', onToggle);

  // Return a disposer so the Blazor component can clean up listeners on teardown.
  return {
    dispose() {
      overlay.removeEventListener('click', collapse);
      if (toggleRegions) toggleRegions.removeEventListener('change', onToggle);
      clearIntensityLayer();
      while (overlay.firstChild) overlay.removeChild(overlay.firstChild);
    }
  };
}

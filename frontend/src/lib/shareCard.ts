/**
 * BEH-SHARE-1: a workout as a story-sized PNG (1080x1920): the word TRACKS, the route line, the song covers.
 * The layout maths is pure and tested; only `renderShareCard` / `saveShareCard` touch the canvas and the DOM.
 */

export const CARD_WIDTH = 1080;
export const CARD_HEIGHT = 1920;
/** Most covers one card holds; a longer run shows its first songs. */
export const MAX_COVERS = 12;

export interface Box { x: number; y: number; width: number; height: number }
export interface Point { x: number; y: number }
export interface CardSong { name: string; albumArtUrl: string | null; routeIndex: number | null }

/** Where the route is drawn, and the grid of covers beneath it. */
export const ROUTE_BOX: Box = { x: 120, y: 330, width: 840, height: 840 };
const COVER_AREA: Box = { x: 90, y: 1260, width: 900, height: 560 };
const COLUMNS = 4;
const GAP = 24;

/** Fits [lat, lng] points into the box, equirectangular and squashed by latitude so a run keeps its shape. */
export const projectRoute = (route: number[][], box: Box = ROUTE_BOX): Point[] => {
  if (route.length === 0) return [];
  const squash = Math.cos((route.reduce((sum, [lat]) => sum + lat, 0) / route.length) * (Math.PI / 180));
  const xs = route.map(([, lng]) => lng * squash);
  const ys = route.map(([lat]) => -lat);
  const [minX, maxX, minY, maxY] = [Math.min(...xs), Math.max(...xs), Math.min(...ys), Math.max(...ys)];
  const spanX = maxX - minX || 1e-9;
  const spanY = maxY - minY || 1e-9;
  const scale = Math.min(box.width / spanX, box.height / spanY);
  const offsetX = box.x + (box.width - spanX * scale) / 2;
  const offsetY = box.y + (box.height - spanY * scale) / 2;
  return xs.map((x, i) => ({ x: offsetX + (x - minX) * scale, y: offsetY + (ys[i] - minY) * scale }));
};

/** Square cover slots, COLUMNS to a row, each row centred; the whole grid fits the cover area. */
export const layoutCovers = (count: number, area: Box = COVER_AREA): Box[] => {
  const n = Math.min(count, MAX_COVERS);
  if (n === 0) return [];
  const rows = Math.ceil(n / COLUMNS);
  const size = Math.min((area.width - GAP * (COLUMNS - 1)) / COLUMNS, (area.height - GAP * (rows - 1)) / rows);
  const slots: Box[] = [];
  for (let i = 0; i < n; i++) {
    const row = Math.floor(i / COLUMNS);
    const inRow = Math.min(COLUMNS, n - row * COLUMNS);
    const rowWidth = inRow * size + (inRow - 1) * GAP;
    slots.push({
      x: area.x + (area.width - rowWidth) / 2 + (i % COLUMNS) * (size + GAP),
      y: area.y + row * (size + GAP),
      width: size,
      height: size,
    });
  }
  return slots;
};

const loadImage = (url: string) =>
  new Promise<HTMLImageElement | null>((resolve) => {
    const img = new Image();
    // Spotify's image host answers with access-control-allow-origin: *, so an anonymous request keeps the canvas exportable.
    img.crossOrigin = 'anonymous';
    img.onload = () => resolve(img);
    img.onerror = () => resolve(null);
    img.src = url;
  });

/** Draws the card on a new 1080x1920 canvas. A cover that will not load is a grey square, never a failed card. */
export const renderShareCard = async (route: number[][], songs: CardSong[]): Promise<HTMLCanvasElement> => {
  const canvas = document.createElement('canvas');
  canvas.width = CARD_WIDTH;
  canvas.height = CARD_HEIGHT;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('No canvas support.');

  ctx.fillStyle = '#000000';
  ctx.fillRect(0, 0, CARD_WIDTH, CARD_HEIGHT);

  ctx.fillStyle = '#ffffff';
  ctx.font = '800 120px system-ui, -apple-system, "Segoe UI", sans-serif';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillText('TRACKS', CARD_WIDTH / 2, 190);

  const points = projectRoute(route);
  if (points.length > 1) {
    ctx.strokeStyle = '#ffffff';
    ctx.lineWidth = 10;
    ctx.lineJoin = 'round';
    ctx.lineCap = 'round';
    ctx.beginPath();
    points.forEach((p, i) => (i === 0 ? ctx.moveTo(p.x, p.y) : ctx.lineTo(p.x, p.y)));
    ctx.stroke();
  }

  const shown = songs.slice(0, MAX_COVERS);
  const slots = layoutCovers(shown.length);
  const images = await Promise.all(shown.map((s) => (s.albumArtUrl ? loadImage(s.albumArtUrl) : Promise.resolve(null))));

  // Numbered dots on the route, matching the numbers on the covers below.
  ctx.font = '700 30px system-ui, -apple-system, "Segoe UI", sans-serif';
  shown.forEach((song, i) => {
    const at = song.routeIndex === null ? undefined : points[song.routeIndex];
    if (!at) return;
    ctx.fillStyle = '#ffffff';
    ctx.beginPath();
    ctx.arc(at.x, at.y, 24, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = '#000000';
    ctx.fillText(String(i + 1), at.x, at.y + 1);
  });

  shown.forEach((_, i) => {
    const slot = slots[i];
    const img = images[i];
    if (img) {
      ctx.drawImage(img, slot.x, slot.y, slot.width, slot.height);
    } else {
      ctx.fillStyle = '#2a2a2a';
      ctx.fillRect(slot.x, slot.y, slot.width, slot.height);
    }
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(slot.x, slot.y, 52, 52);
    ctx.fillStyle = '#000000';
    ctx.font = '700 32px system-ui, -apple-system, "Segoe UI", sans-serif';
    ctx.fillText(String(i + 1), slot.x + 26, slot.y + 27);
  });

  return canvas;
};

const toBlob = (canvas: HTMLCanvasElement) =>
  new Promise<Blob>((resolve, reject) =>
    canvas.toBlob((blob) => (blob ? resolve(blob) : reject(new Error('Could not make the image.'))), 'image/png'),
  );

/** Renders the card and hands it over: the share sheet where the browser can share files, else a PNG download. */
export const saveShareCard = async (name: string, route: number[][], songs: CardSong[]): Promise<void> => {
  const blob = await toBlob(await renderShareCard(route, songs));
  const file = new File([blob], 'tracks-share.png', { type: 'image/png' });
  if (typeof navigator.canShare === 'function' && navigator.canShare({ files: [file] })) {
    try {
      await navigator.share({ files: [file], title: name });
      return;
    } catch (e) {
      if ((e as Error).name === 'AbortError') return; // the runner closed the sheet; that is an answer
    }
  }
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'tracks-share.png';
  a.click();
  URL.revokeObjectURL(url);
};

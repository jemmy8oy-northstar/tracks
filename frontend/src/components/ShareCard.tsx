import { useState } from 'react';
import { Button } from './ui';
import { renderShareCard, saveShareCard, type CardSong } from '../lib/shareCard';

/**
 * BEH-SHARE-1: a Share button that draws the run as a story-sized card (shown here as a preview) the runner can save.
 * Rendering happens on the first click; saving is a second click so the browser's share sheet gets a fresh gesture.
 */
const ShareCard = ({ name, route, songs }: { name: string; route: number[][]; songs: CardSong[] }) => {
  const [preview, setPreview] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);

  const show = () => {
    setBusy(true);
    setFailed(false);
    renderShareCard(route, songs)
      .then((canvas) => setPreview(canvas.toDataURL('image/png')))
      .catch(() => setFailed(true))
      .finally(() => setBusy(false));
  };

  const save = () => {
    setFailed(false);
    saveShareCard(name, route, songs).catch(() => setFailed(true));
  };

  return (
    <div style={{ margin: '8px 0' }}>
      {preview === null ? (
        <Button variant="secondary" size="sm" loading={busy} onClick={show}>
          Share
        </Button>
      ) : (
        <section aria-label="Share card" data-testid="share-card">
          <img src={preview} alt={`Share card for ${name}`} style={{ width: '200px', maxWidth: '100%', display: 'block', borderRadius: '8px' }} />
          <div style={{ display: 'flex', gap: '8px', marginTop: '8px' }}>
            <Button variant="primary" size="sm" onClick={save}>Save image</Button>
            <Button variant="ghost" size="sm" onClick={() => setPreview(null)}>Close</Button>
          </div>
        </section>
      )}
      {failed && <p role="alert" style={{ color: '#f87171', fontSize: '0.9rem', marginTop: '8px' }}>Could not make the share card. Please try again.</p>}
    </div>
  );
};

export default ShareCard;

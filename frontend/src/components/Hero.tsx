import React from 'react';

interface HeroProps {
    /** Replaces the default call to action. */
    children?: React.ReactNode;
}

const Hero: React.FC<HeroProps> = ({ children }) => {
    return (
        <section className="container" style={{
            textAlign: 'center',
            marginTop: '60px'
        }}>
            <h1 style={{
                fontSize: 'clamp(3.5rem, 8vw, 6rem)',
                marginBottom: '24px',
                background: 'linear-gradient(to bottom right, var(--text-primary) 30%, rgba(148, 163, 184, 0.5))',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                lineHeight: 1.1
            }}>
                Tracks
            </h1>
            <p style={{
                fontSize: 'clamp(1.1rem, 2vw, 1.4rem)',
                color: 'var(--text-secondary)',
                maxWidth: '600px',
                margin: '0 auto 48px',
                fontWeight: 400
            }}>
                Track your tracks' tracks. The songs you listened to, pinned along the route you ran.
            </p>
            {children}
        </section>
    );
};

export default Hero;

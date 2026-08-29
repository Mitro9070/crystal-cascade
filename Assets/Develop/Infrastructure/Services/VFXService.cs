using UnityEngine;

namespace NeonSeven.Infrastructure.Services
{
    public sealed class VFXService
    {
        public void PlayMatchBurst(Vector2 screenPosition)
        {
            // The UI view currently owns visual-only shockwaves; this service is the expansion point for pooled ParticleSystem VFX.
        }
    }
}

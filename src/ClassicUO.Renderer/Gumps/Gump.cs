using System;
using ClassicUO.Assets;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer.Gumps
{
    public sealed class Gump
    {
        private const long FailedRetryIntervalMs = 5000;

        private readonly TextureAtlas _atlas;
        private readonly SpriteInfo[] _spriteInfos;
        private readonly bool[] _failedSprites;
        private readonly PixelPicker _picker = new PixelPicker();
        private readonly GumpsLoader _gumpsLoader;
        private long _nextFailedRetryAt;

        public Gump(GumpsLoader gumpsLoader, GraphicsDevice device)
        {
            _gumpsLoader = gumpsLoader;
            _atlas = new TextureAtlas(device, 4096, 4096, SurfaceFormat.Color);
            _spriteInfos = new SpriteInfo[gumpsLoader.File.Entries.Length];
            _failedSprites = new bool[gumpsLoader.File.Entries.Length];
        }

        public ref readonly SpriteInfo GetGump(uint idx)
        {
            if (idx >= _spriteInfos.Length)
                return ref SpriteInfo.Empty;

            if (_failedSprites[idx])
            {
                // Sprites can become available later (patched MULs), but re-reading and
                // decompressing them on every call turns a missing gump into a per-frame cost.
                if (Environment.TickCount64 < _nextFailedRetryAt)
                {
                    return ref SpriteInfo.Empty;
                }

                _nextFailedRetryAt = Environment.TickCount64 + FailedRetryIntervalMs;

                if (_gumpsLoader.GetGump(idx).Pixels.IsEmpty)
                {
                    return ref SpriteInfo.Empty;
                }

                _failedSprites[idx] = false;
            }

            ref var spriteInfo = ref _spriteInfos[idx];

            if (spriteInfo.Texture == null)
            {
                var gumpInfo = _gumpsLoader.GetGump(idx);
                if (!gumpInfo.Pixels.IsEmpty)
                {
                    spriteInfo.Texture = _atlas.AddSprite(
                        gumpInfo.Pixels,
                        gumpInfo.Width,
                        gumpInfo.Height,
                        out spriteInfo.UV
                    );

                    _picker.Set(idx, gumpInfo.Width, gumpInfo.Height, gumpInfo.Pixels);
                }
                else
                {
                    _failedSprites[idx] = true;
                    return ref SpriteInfo.Empty;
                }
            }

            return ref spriteInfo;
        }

        public bool PixelCheck(uint idx, int x, int y) => _picker.Get(idx, x, y);
    }
}

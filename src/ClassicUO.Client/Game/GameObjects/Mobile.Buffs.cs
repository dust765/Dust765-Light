// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Game.Data;

namespace ClassicUO.Game.GameObjects
{
    internal partial class Mobile
    {
        private readonly Dictionary<BuffIconType, BuffIcon> _buffIcons = new Dictionary<BuffIconType, BuffIcon>();

        public IReadOnlyDictionary<BuffIconType, BuffIcon> BuffIcons => _buffIcons;

        public void AddBuff(BuffIconType type, ushort graphic, uint time, string text)
        {
            _buffIcons[type] = new BuffIcon(type, graphic, time, text);
        }

        public bool IsBuffIconExists(BuffIconType type)
        {
            return _buffIcons.ContainsKey(type);
        }

        public void RemoveBuff(BuffIconType type)
        {
            _buffIcons.Remove(type);
        }

        public void ClearBuffs()
        {
            _buffIcons.Clear();
        }
    }
}

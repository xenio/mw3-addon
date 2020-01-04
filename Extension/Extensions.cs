// Fix for MW3 1.9.388 <|DSC|>Xenio
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Addon
{
    static class Extensions
    {
        #region Delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void CloneBrushModelToScriptModelFuncDelegate(IntPtr scriptModelEntity, IntPtr brushModelEntity);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int LoadFxFuncDelegate(string fxName);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PlayFxFuncDelegate(int fxNum, ref Vec3 origin, IntPtr forward, IntPtr up);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SvLinkEntityDelegate(IntPtr entityAddress);
        #endregion

        #region Stub
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private static readonly byte[] GetEspValueStub = new byte[]
        {
            0x8B, 0x04, 0x24, // mov eax, [esp]
            0xC3 // retn
        };

        private static readonly byte[] CloneBrushModelToScriptModelFuncStub = new byte[]
        {
            0x55, // push ebp
            0x8B, 0xEC, // mov ebp, esp
            0x53, // push ebx
            0x56, // push esi
            0x57, // push edi
            0x60, // pushad
            0x8B, 0x75, 0x08, // mov esi, [ebp+8]
            0x8B, 0x45, 0x0C, // mov eax, [ebp+C]
            0x8B, 0xF8, // mov edi, eax
            0xE8, 0x00, 0x00, 0x00, 0x00, // call <relative distance>
            0x83, 0xC0, 0x0E, // add eax, 0xE
            0x50, // push eax
            0x8B, 0xC7, // mov eax, edi
            0x56, // push esi
            0x57, // push edi
            0xFF, 0x25, 0x00, 0x00, 0x00, 0x00, // jmp [address containing value]
            0x61, // popad
            0x5F, // pop edi
            0x5E, // pop esi
            0x5B, // pop ebx
            0x5D, // pop ebp
            0xC3 // retn
        };

        private static readonly byte[] LoadFxFuncStub = new byte[]
        {
            0x55, // push ebp
            0x8B, 0xEC, // mov ebp, esp
            0x51, // push ecx
            0x57, // push edi
            0xC7, 0x45, 0xFC, 0x80, 0x42, 0x4B, 0x00, // mov dword ptr [ebp-8], 4B4280
            0x8B, 0x7D, 0x08, // mov edi, [ebp+8]
            0xFF, 0x55, 0xFC, // call [ebp-4]
            0x5F, // pop edi
            0x8B, 0xE5, // mov esp, ebp
            0x5D, // pop ebp
            0xC3 // retn
        };

        private static readonly byte[] PlayFxFuncStub = new byte[]
        {
            0x55, // push ebp
            0x8B, 0xEC, // mov ebp, esp
            0x51, // push ecx
            0xC7, 0x45, 0xFC, 0xB0, 0x0B, 0x4A, 0x00, // mov dword ptr [ebp-4], 4A0BB0
            0x8B, 0x45, 0x10, // mov eax, [ebp+10]
            0x8B, 0x4D, 0x08, // mov ecx, [ebp+8]
            0xFF, 0x75, 0x14, // push [ebp+14]
            0xFF, 0x75, 0x0C, // push [ebp+C]
            0xFF, 0x55, 0xFC, // call [ebp-4]
            0x83, 0xC4, 0x08, // add esp, 8
            0x8B, 0xE5, // mov esp, ebp
            0x5D, // pop ebp
            0xC3 // retn
        };

        private static readonly byte[] SvLinkEntityFuncStub = new byte[]
        {
            0x55, // push ebp
            0x8B, 0xEC, // mov ebp, esp
            0x56, // push esi
            0x8B, 0x75, 0x08, // mov esi, [ebp+8]
            0xC7, 0x45, 0xFC, 0x30, 0x38, 0x50, 0x00, // mov dword ptr [ebp-4], 503830
            0xFF, 0x55, 0xFC, // call [ebp-4]
            0x5E, // pop esi
            0x8B, 0xE5, // mov esp, ebp
            0x5D, // pop ebp
            0xC3 // retn
        };
        #endregion

        [StructLayout(LayoutKind.Sequential)]
        struct Vec3
        {
            public float X;
            public float Y;
            public float Z;

            public static implicit operator Vec3(Vector vector)
            {
                return new Vec3 { X = vector.X, Y = vector.Y, Z = vector.Z };
            }

            public static implicit operator Vector(Vec3 vector)
            {
                return new Vector(vector.X, vector.Y, vector.Z);
            }
        }

        private static IntPtr _cloneBrushModelToScriptModelFuncAddress = IntPtr.Zero;
        private static IntPtr _loadFxFuncAddress = IntPtr.Zero;
        private static IntPtr _playFxFuncAddress = IntPtr.Zero;
        private static IntPtr _getEspValueAddress = IntPtr.Zero;
        private static IntPtr _svLinkEntityAddress = IntPtr.Zero;
        private static string _lastMapName = string.Empty;
        private static int _lastAirdropCrateCollisionId = -1;

        private static CloneBrushModelToScriptModelFuncDelegate _cloneBrushModelToScriptModelFunc;
        private static LoadFxFuncDelegate _loadFxFunc;
        private static PlayFxFuncDelegate _playFxFunc;
        private static SvLinkEntityDelegate _svLinkEntityFunc;

        private static readonly IntPtr EntityAddress = (IntPtr)0x191B900;// Funziona!
        private static readonly IntPtr JmpDestination = (IntPtr)0x4AFD78;// Funziona!
        private static readonly IntPtr D3DBspEntsPointer = (IntPtr)0x17A4C50;// Funziona!
        private static readonly IntPtr MapNameDvarPointer = (IntPtr)0x4FA6D8;// Nulled, non va, fixato a codice con pointer statico
        private static readonly IntPtr ScriptFailuireByteCheckAddress = (IntPtr)0x1CD5724;// Non va, da cercare nuovo offset.

        private static IntPtr GetEntityFromNum(int entityNum)
        {
            return (IntPtr)(EntityAddress.ToInt32() + entityNum * 0x274);
        }

        public static int SetContents(Entity entity, int contents)
        {
            var entityAddress = GetEntityFromNum(entity.EntityNum);
            var oldContents = Marshal.ReadInt32(entityAddress, 0x11C);
            Marshal.WriteInt32(entityAddress, 0x11C, contents);

            if (_svLinkEntityAddress == IntPtr.Zero)
            {
                _svLinkEntityAddress = VirtualAlloc(IntPtr.Zero, (UIntPtr)SvLinkEntityFuncStub.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (_svLinkEntityAddress == IntPtr.Zero)
                    return oldContents;
                Marshal.Copy(SvLinkEntityFuncStub, 0, _svLinkEntityAddress, SvLinkEntityFuncStub.Length);
                _svLinkEntityFunc = (SvLinkEntityDelegate)Marshal.GetDelegateForFunctionPointer(_svLinkEntityAddress, typeof(SvLinkEntityDelegate));
            }

            _svLinkEntityFunc(entityAddress);
            return oldContents;
        }

        public static void Show(Entity entity)
        {
            IntPtr entityAddress = GetEntityFromNum(entity.EntityNum);
            Marshal.WriteInt32(entityAddress, 0x8, Marshal.ReadInt32(entityAddress, 0x8) & ~0x20);
            Marshal.WriteInt32(entityAddress, 0xFC, 0);
        }

        public static void Hide(Entity entity)
        {
            IntPtr entityAddress = GetEntityFromNum(entity.EntityNum);
            Marshal.WriteInt32(entityAddress, 0x8, Marshal.ReadInt32(entityAddress, 0x8) | 0x20);
            Marshal.WriteInt32(entityAddress, 0xFC, -1);
        }

        public static Vector GetAngles(ServerClient client)
        {
            return GetAngles(client.ClientNum);
        }

        public static Vector GetAngles(Entity entity)
        {
            return GetAngles(entity.EntityNum);
        }

        private static Vector GetAngles(int entityNum)
        {
            int entityAddress = GetEntityFromNum(entityNum).ToInt32();
            return (Vec3)Marshal.PtrToStructure((IntPtr)(entityAddress + 0x3C), typeof(Vec3));
        }

        public static void SetAngles(ServerClient client, Vector angles)
        {
            SetAngles(client.ClientNum, angles);
        }

        public static void SetAngles(Entity entity, Vector angles)
        {
            SetAngles(entity.EntityNum, angles);
        }

        private static void SetAngles(int entityNum, Vector angles)
        {
            int entityAddress = GetEntityFromNum(entityNum).ToInt32();

            var angle = new Vec3 { X = angles.X, Y = angles.Y, Z = angles.Z };
            Marshal.StructureToPtr(angle, (IntPtr)(entityAddress + 0x144), false);
            Marshal.StructureToPtr(angle, (IntPtr)(entityAddress + 0x3C), false);
        }

        public static int LoadFX(string fxName)
        {
            // Allocate the stub/function.
            if (_loadFxFuncAddress == IntPtr.Zero)
            {
                _loadFxFuncAddress = VirtualAlloc(IntPtr.Zero, (UIntPtr)LoadFxFuncStub.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (_loadFxFuncAddress == IntPtr.Zero)
                    return 0;
                Marshal.Copy(LoadFxFuncStub, 0, _loadFxFuncAddress, LoadFxFuncStub.Length);
                _loadFxFunc = (LoadFxFuncDelegate)Marshal.GetDelegateForFunctionPointer(_loadFxFuncAddress, typeof(LoadFxFuncDelegate));
            }

            // Save and override a byte value to bypass longjmp in-case of failiure.
            byte value = Marshal.ReadByte(ScriptFailuireByteCheckAddress);
            Marshal.WriteByte(ScriptFailuireByteCheckAddress, 1);

            // Call the LoadFX stub (which calls G_EffectIndex)
            int result = _loadFxFunc(fxName);

            // Restore the byte value.
            Marshal.WriteByte(ScriptFailuireByteCheckAddress, value);

            // Return effect ID.
            return result;
        }

        public static void PlayFX(int effectId, Vector origin, Vector forward = null, Vector up = null)
        {
            // Allocate the stub/function.
            if (_playFxFuncAddress == IntPtr.Zero)
            {
                _playFxFuncAddress = VirtualAlloc(IntPtr.Zero, (UIntPtr)PlayFxFuncStub.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (_playFxFuncAddress == IntPtr.Zero)
                    return;
                Marshal.Copy(PlayFxFuncStub, 0, _playFxFuncAddress, PlayFxFuncStub.Length);
                _playFxFunc = (PlayFxFuncDelegate)Marshal.GetDelegateForFunctionPointer(_playFxFuncAddress, typeof(PlayFxFuncDelegate));
            }

            // Since forward and up are optional, set them to NULL if they are not used.
            IntPtr forwardPtr = IntPtr.Zero, upPtr = IntPtr.Zero;

            // Convert the forward/up vector to a pointer.
            if (forward != null) forwardPtr = GetPtrFromObject(forward);
            if (up != null) upPtr = GetPtrFromObject(up);

            // Call the function.
            var vec3 = (Vec3)origin;
            _playFxFunc(effectId, ref vec3, forwardPtr, upPtr);

            // Free the memory if required.
            if (forwardPtr != IntPtr.Zero) Marshal.FreeHGlobal(forwardPtr);
            if (upPtr != IntPtr.Zero) Marshal.FreeHGlobal(upPtr);
        }

        private static IntPtr GetPtrFromObject(object obj)
        {
            int size = Marshal.SizeOf(obj);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(obj, ptr, true);
            return ptr;
        }


        public static int FindAirdropCrateCollisionId()
        {
            try
            {
                IntPtr mapNameDvarAddress = Marshal.ReadIntPtr(MapNameDvarPointer); // originale senza offset
                IntPtr Mappa = (IntPtr)0x1FBE39D;
                if (mapNameDvarAddress == IntPtr.Zero)
                    return -1;
                // string currentMap = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(mapNameDvarAddress, 0xC));
                string currentMap = Marshal.PtrToStringAnsi(Mappa); // 0xC));


                if (currentMap == _lastMapName)
                    return _lastAirdropCrateCollisionId;

                IntPtr d3dBspEntsAddress = Marshal.ReadIntPtr(D3DBspEntsPointer);
                if (d3dBspEntsAddress == IntPtr.Zero)
                    return -1;

                IntPtr airdropCrateTargetName = FindStringPattern(d3dBspEntsAddress, Encoding.ASCII.GetBytes("1673 \"care_package\""));
                IntPtr startEntEntry = airdropCrateTargetName;
                while (Marshal.ReadByte(startEntEntry) != '{')
                {
                    startEntEntry = (IntPtr)(startEntEntry.ToInt32() - 1);
                }

                var targetAddress = (IntPtr)(FindStringPattern(startEntEntry, Encoding.ASCII.GetBytes("1672 \"")).ToInt32() + 7);
                var endTargetNameAddress = FindStringPattern(targetAddress, Encoding.ASCII.GetBytes("\""));
                string target = Marshal.PtrToStringAnsi(targetAddress, endTargetNameAddress.ToInt32() - targetAddress.ToInt32() + 1);
                IntPtr airdropCrateCollisionTargetName = FindStringPattern(d3dBspEntsAddress, Encoding.ASCII.GetBytes("1673 \"" + target + "\""));
                startEntEntry = airdropCrateCollisionTargetName;
                while (Marshal.ReadByte(startEntEntry) != '{')
                {
                    startEntEntry = (IntPtr)(startEntEntry.ToInt32() - 1);
                }

                var airdropCrateCollisionOrigin = (IntPtr)(FindStringPattern(startEntEntry, Encoding.ASCII.GetBytes("1669 \"")).ToInt32() + 7);
                var endOriginAddress = FindStringPattern(airdropCrateCollisionOrigin, Encoding.ASCII.GetBytes("\""));

                string originValue = Marshal.PtrToStringAnsi(airdropCrateCollisionOrigin, endOriginAddress.ToInt32() - airdropCrateCollisionOrigin.ToInt32() + 1);
                string[] origin = originValue.Split(' ');

                float x = float.Parse(origin[0]);
                float y = float.Parse(origin[1]);
                float z = float.Parse(origin[2]);
                // Verificare riferimento decompilato addon.dll cPlugin linea 313
                for (int i = 18; i < 2047; i++)
                {
                    var currentOrigin = (Vec3)Marshal.PtrToStructure((IntPtr)(GetEntityFromNum(i).ToInt32() + 0x18), typeof(Vec3));
                    if (currentOrigin.X != x || currentOrigin.Y != y || currentOrigin.Z != z)
                        continue;
                    _lastAirdropCrateCollisionId = i;
                    _lastMapName = currentMap;
                    return i;
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        private static IntPtr FindStringPattern(IntPtr startAddress, byte[] value)
        {
            int offset = 0, matchIndex = 0;
            byte read;
            while ((read = Marshal.ReadByte(startAddress, offset)) != 0)
            {
                if (value[matchIndex++] == read)
                {
                    if (matchIndex == value.Length)
                        return (IntPtr)(startAddress.ToInt32() + offset - value.Length);
                }
                else
                {
                    matchIndex = 0;
                }
                offset++;
            }
            return IntPtr.Zero;
        }

        public static void CloneBrushModelToScriptModel(Entity scriptModel, int brushModelEntityId)
        {
            if (_cloneBrushModelToScriptModelFuncAddress == IntPtr.Zero)
            {

                // Allocate memory for the stubs/functions.
                _cloneBrushModelToScriptModelFuncAddress = VirtualAlloc(IntPtr.Zero, (UIntPtr)CloneBrushModelToScriptModelFuncStub.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                _getEspValueAddress = VirtualAlloc(IntPtr.Zero, (UIntPtr)GetEspValueStub.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (_cloneBrushModelToScriptModelFuncAddress == IntPtr.Zero || _getEspValueAddress == IntPtr.Zero)
                    return;

                // Allocate memory for a integer holding the jmp destination address. 
                IntPtr jmpDestinationHolder = Marshal.AllocHGlobal(4);
                Marshal.Copy(BitConverter.GetBytes(JmpDestination.ToInt32()), 0, jmpDestinationHolder, 4);
                // Fix the stub with the address of the GetEspValue stub that we just allocated since it's a relative call.
                Array.Copy(BitConverter.GetBytes(_getEspValueAddress.ToInt32() - _cloneBrushModelToScriptModelFuncAddress.ToInt32() - 20), 0, CloneBrushModelToScriptModelFuncStub, 16, 4);
                // Do the same for the jmp destination.
                Array.Copy(BitConverter.GetBytes(jmpDestinationHolder.ToInt32()), 0, CloneBrushModelToScriptModelFuncStub, 30, 4);
                // Write the bytes for stubs/functions to the memory we allocated earlier on.
                Marshal.Copy(CloneBrushModelToScriptModelFuncStub, 0, _cloneBrushModelToScriptModelFuncAddress, CloneBrushModelToScriptModelFuncStub.Length);
                Marshal.Copy(GetEspValueStub, 0, _getEspValueAddress, GetEspValueStub.Length);

                // Produce a delegate to invoke/call.
                _cloneBrushModelToScriptModelFunc = (CloneBrushModelToScriptModelFuncDelegate)Marshal.GetDelegateForFunctionPointer(_cloneBrushModelToScriptModelFuncAddress, typeof(CloneBrushModelToScriptModelFuncDelegate));
            }
            _cloneBrushModelToScriptModelFunc(GetEntityFromNum(scriptModel.EntityNum), GetEntityFromNum(brushModelEntityId));
        }
    }
}
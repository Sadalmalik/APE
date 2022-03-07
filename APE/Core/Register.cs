namespace Processor
{
	public class Register
	{
		public string Name { get; }
		private ulong _value;

		public Register(string name)
		{
			Name = name;
		}
		
		public void Inc()
		{
			_value++;
		}

		public void Dec()
		{
			_value--;
		}
		
		public long Value
		{
			get => (long)_value;
			set => _value = (ulong)value;
		}
		
		public ulong QWORD
		{
			get => _value;
			set => _value = value;
		}

		public uint uDWORD
		{
			get => (uint) ((_value & 0x0000_0000_FFFF_FFFF) >> 32);
			set => _value = (_value & 0x0000_0000_FFFF_FFFF) + (value << 32);
		}

		public uint dDWORD
		{
			get => (uint) (_value & 0x0000_0000_FFFF_FFFF);
			set => _value = (_value & 0xFFFF_FFFF_0000_0000) + value;
		}

		public ushort uWORD
		{
			get => (ushort) ((_value & 0x0000_0000_FFFF_0000) >> 16);
			set => _value = (_value & 0xFFFF_FFFF_0000_FFFF) + (ushort) (value << 16);
		}

		public ushort dWORD
		{
			get => (ushort) (_value & 0x0000_0000_0000_FFFF);
			set => _value = (_value & 0xFFFF_FFFF_FFFF_0000) + value;
		}

		public byte uBYTE
		{
			get => (byte) ((_value & 0x0000_0000_0000_FF00) >> 8);
			set => _value = (_value & 0xFFFF_FFFF_FFFF_00FF) + (byte) (value << 8);
		}

		public byte dBYTE
		{
			get => (byte) (_value & 0x0000_0000_0000_00FF);
			set => _value = (_value & 0xFFFF_FFFF_FFFF_FF00) + value;
		}

		public bool GetFlag(int index)
		{
			return 0 != ((_value >> index) & 1);
		}

		public void SetFlag(int index, bool value)
		{
			if (value)
				_value |= (ulong) (1 << index);
			else
				_value &= (ulong) ~(1 << index);
		}

		public static void Move(Register from, Register into, int size)
		{
			switch (size)
			{
				case Size.Byte:	into.dBYTE = from.dBYTE; break;
				case Size.Word: into.dWORD = from.dWORD; break;
				case Size.DWord: into.dDWORD = from.dDWORD; break;
				case Size.QWord: into.QWORD = from.QWORD; break;
			}
		}
	}
}
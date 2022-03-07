using System;
using System.Text;

namespace Processor
{
	public static class Asm
	{
		public const int RM0 = 0;
		public const int RM1 = 1;
		public const int RM2 = 2;
		public const int RM3 = 3;

		public const int RS0 = 4;
		public const int RS1 = 5;
		public const int RS2 = 6;
		public const int RS3 = 7;

		public const int RIP = 8;
		public const int RDC = 9;
		public const int RDR = 10;
		public const int RPF = 11;

		public const int RTX = 12;
		public const int RLM = 13;
		public const int RHM = 14;

		/*
				RM0, RM1, RM2, RM3,
				RS0, RS1, RS2, RS3,
				RIP, RDC, RDR, RPF,
				RTX, RLM, RHM
		*/

		public static ulong JumpOffset(sbyte value)
		{
			ulong uvalue          = (ulong) Math.Abs(value);
			if (value < 0) uvalue = ~uvalue + 1;
			return uvalue;
		}

		public static ulong JumpOffset(short value)
		{
			ulong uvalue          = (ulong) Math.Abs(value);
			if (value < 0) uvalue = ~uvalue + 1;
			return uvalue;
		}

		public static ulong JumpOffset(int value)
		{
			ulong uvalue          = (ulong) Math.Abs(value);
			if (value < 0) uvalue = ~uvalue + 1;
			return uvalue;
		}

		public static ulong JumpOffset(long value)
		{
			return (ulong) value;
		}
	}

	/// <summary>
	/// Класс для упрощённой генерации кода
	/// Для полноценной компиляции нужен ещё парсер и логика работы с переменными (метками/адресами) и всё!
	/// </summary>
	public class Assembler
	{
		private byte[] _memory;
		private int    _offset;

		public ulong Offset
		{
			get => (ulong) _offset;
			set => _offset = (int) value;
		}

		public Assembler(byte[] memory, int offset = 0)
		{
			_memory = memory;
			_offset = offset;
		}

		public Assembler GetOffset(out ulong offset)
		{
			offset = Offset;
			return this;
		}

		public readonly char[] hex = "0123456789ABCDEF".ToCharArray();

		public Assembler AddByte(byte value)
		{
			Console.Write(hex[(value >> 4) & 0xF]);
			Console.Write(hex[(value >> 0) & 0xF]);
			Console.Out.Flush();
			_memory[_offset++] = value;
			return this;
		}

		public Assembler AddString(string str)
		{
			Console.Write($"{Offset:X8} : string \"{str}\"\n");
			Console.Out.Flush();
			byte[] content = Encoding.UTF8.GetBytes(str);
			AddValue(Size.DWord, (ulong) content.Length);
			Console.Write($"{Offset:X8} : raw ");
			Console.Out.Flush();
			for (int i = 0; i < content.Length; i++)
				AddByte(content[i]);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		public Assembler AddValue(int size, ulong constant)
		{
			Console.Write($"{Offset:X8} : value ");
			size &= 0b11;
			int count = (int) Size.SizeToStep[size];

			for (int i = 0; i < count; i++)
				AddByte((byte) (constant >> (8 * (count - i - 1)) & 0xFF));

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		public Assembler FillUntil(int end)
		{
			while(_offset < end)
			{
				_memory[_offset] = 0;
				_offset++;
			}

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		public Assembler NOP()
		{
			Console.Write($"{Offset:X8} : NOP ");
			Console.Out.Flush();
			AddByte(0b000_000_00);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		/*
			100 000 00  MOV reg, reg
			100 001 00  MOV reg, [reg]
			100 010 00  MOV [reg], reg
			100 011 00  MOV [reg], [reg]
		*/
		public Assembler MOV(
			int  size,
			int  lReg, int  rReg,
			bool lRef, bool rRef)
		{
			Console.Write($"{Offset:X8} : MOV ");
			Console.Out.Flush();
			byte cmd = 0b100_000_00;
			cmd |= (byte) (size & 0b11);
			cmd |= (byte) (lRef ? 0b000_010_00 : 0);
			cmd |= (byte) (rRef ? 0b000_001_00 : 0);
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((lReg & 0xF) << 4);
			regs |= (byte) ((rReg & 0xF) << 0);
			AddByte(regs);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		/*
			100 100 00  MOV const, reg
			100 101 00  MOV const, [reg]
		*/
		public Assembler MOV_ASSIGN(
			int   size,
			int   reg, bool isRef,
			ulong constant)
		{
			Console.Write($"{Offset:X8} : MOV_ASSIGN ");
			Console.Out.Flush();
			byte cmd = 0b100_100_00;
			cmd |= (byte) (size & 0b11);
			cmd |= (byte) (isRef ? 0b000_001_00 : 0);
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((reg & 0xF) << 0);
			AddByte(regs);

			AddValue(size, constant);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		/*
			100 110 00  MOV reg, [const] // Как иначе писать в предзаданные переменные?
			100 111 00  MOV [const], reg
		*/
		public Assembler MOV_VARIABLE(
			int   size,
			int   reg,
			ulong address,
			bool  constLeft)
		{
			Console.Write($"{Offset:X8} : MOV_VAR ");
			Console.Out.Flush();
			byte cmd = 0b100_110_00;
			cmd |= (byte) (size & 0b11);
			cmd |= (byte) (constLeft ? 0b000_001_00 : 0);
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((reg & 0xF) << 0);
			AddByte(regs);

			AddValue(size, address);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		/*
		010 000 00  PUSH reg
		010 001 00  POP reg
		010 010 00  PUSH reg, <reg>
		010 011 00  POP reg, <reg>
		*/
		public Assembler Push(
				int size,
				int lReg, int rReg = 4)
			// 4 - регистр RS0 в текущей архитектуре
		{
			Console.Write($"{Offset:X8} : PUSH ");
			Console.Out.Flush();
			byte cmd = 0b010_000_00;
			cmd |= (byte) (size & 0b11);
			cmd |= (byte) (rReg != 4 ? 0b000_010_00 : 0);
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((lReg & 0xF) << 4);
			regs |= (byte) ((rReg & 0xF) << 0);
			AddByte(regs);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		public Assembler Pop(
				int size,
				int lReg, int rReg = 4)
			// 4 - регистр RS0 в текущей архитектуре
		{
			Console.Write($"{Offset:X8} : POP ");
			Console.Out.Flush();
			byte cmd = 0b010_001_00;
			cmd |= (byte) (size & 0b11);
			cmd |= (byte) (rReg != 4 ? 0b000_010_00 : 0);
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((lReg & 0xF) << 4);
			regs |= (byte) ((rReg & 0xF) << 0);
			AddByte(regs);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		/*
		110 000 00  JMP const
		110 001 00  JMP reg
		*/
		public Assembler Jump(
			int   size,
			ulong constant,
			bool  isReg = false)
		{
			Console.Write($"{Offset:X8} : JMP ");
			Console.Out.Flush();
			byte cmd = 0b110_000_00;
			cmd |= (byte) (size & 0b11);
			AddByte(cmd);

			byte regs = 0;
			if (isReg)
				regs |= (byte) ((constant & 0xF) << 0);
			AddByte(regs);

			if (!isReg)
				AddValue(size, constant);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		/*
		110 100 00  JPZ reg, const
		110 101 00  JPZ reg, reg
		110 110 00  JNZ reg, const
		110 111 00  JNZ reg, reg
		*/
		public Assembler JumpCond(
			int   size,
			int   reg,
			ulong constant,
			bool  isReg,
			bool  isZero = true)
		{
			Console.Write($"{Offset:X8} : JMP COND ");
			Console.Out.Flush();
			byte cmd = 0b110_100_00;
			cmd |= (byte) (size & 0b11);
			if (!isZero)
				cmd |= 0b000_010_00;
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((reg & 0xF) << 4);
			if (isReg)
				regs |= (byte) ((constant & 0xF) << 0);
			AddByte(regs);

			if (!isReg)
				AddValue(size, constant);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		/*
		001 000 00  NOT reg
		001 001 00  AND reg, reg
		001 010 00  OR reg, reg
		001 011 00  XOR reg, reg
		*/
		public Assembler BinaryOperation(int size, int lReg, int rReg, int command)
		{
			Console.Write($"{Offset:X8} : BIN ");
			Console.Out.Flush();
			byte cmd = 0b001_000_00;
			cmd |= (byte) (size & 0b11);
			cmd |= (byte) ((command & 0b11) << 2);
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((lReg & 0xF) << 4);
			regs |= (byte) ((rReg & 0xF) << 0);
			AddByte(regs);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		public Assembler NOT(int size, int lReg)           => BinaryOperation(size, lReg, 0, 0b00);
		public Assembler AND(int size, int lReg, int rReg) => BinaryOperation(size, lReg, rReg, 0b01);
		public Assembler OR(int  size, int lReg, int rReg) => BinaryOperation(size, lReg, rReg, 0b10);
		public Assembler XOR(int size, int lReg, int rReg) => BinaryOperation(size, lReg, rReg, 0b11);

		/*
		101 000 00  ADD reg, reg
		101 001 00  SUB reg, reg
		101 010 00  MUL reg, reg
		101 011 00  DIV reg, reg
		101 100 00  MOD reg, reg
		*/
		public Assembler MATH(int size, int lReg, int rReg, int command)
		{
			Console.Write($"{Offset:X8} : MATH ");
			Console.Out.Flush();
			byte cmd = 0b001_000_00;
			cmd |= (byte) (size & 0b11);
			cmd |= (byte) ((command & 0b111) << 2);
			AddByte(cmd);

			byte regs = 0;
			regs |= (byte) ((lReg & 0xF) << 4);
			regs |= (byte) ((rReg & 0xF) << 0);
			AddByte(regs);

			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		public Assembler ADD(int size, int lReg, int rReg) => MATH(size, lReg, rReg, 0b000);
		public Assembler SUB(int size, int lReg, int rReg) => MATH(size, lReg, rReg, 0b001);
		public Assembler MUL(int size, int lReg, int rReg) => MATH(size, lReg, rReg, 0b010);
		public Assembler DIV(int size, int lReg, int rReg) => MATH(size, lReg, rReg, 0b011);
		public Assembler MOD(int size, int lReg, int rReg) => MATH(size, lReg, rReg, 0b100);

		/*
		011 - Однобайтовые
		011 0 0000  INC reg
		011 1 0000  DEC reg
		*/
		public Assembler INC(int reg)
		{ 
			Console.Write($"{Offset:X8} : INC ");
			AddByte((byte) (0b011_0_0000 | (reg & 0b1111)));
			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}
		public Assembler DEC(int reg)
		{ 
			Console.Write($"{Offset:X8} : DEC ");
			AddByte((byte) (0b011_1_0000 | (reg & 0b1111)));
			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}

		public Assembler INT()
		{
			Console.Write($"{Offset:X8} : INT ");
			AddByte((byte) 0xFF);
			Console.Write("\r\n");
			Console.Out.Flush();
			return this;
		}
	}
}
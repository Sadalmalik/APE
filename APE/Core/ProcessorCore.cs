using System;
using System.Collections.Generic;
using System.Numerics;

namespace Processor
{
	public class ProcessorCore
	{
		public readonly Register           DataBus;
		public readonly ProcessorRegisters Registers;

		public readonly Dictionary<int, Device> Devices;

		public bool Completed
		{
			get => Registers.RPF.GetFlag(31);
			set => Registers.RPF.SetFlag(31, value);
		}
		public bool Interrupt
		{
			get => Registers.RPF.GetFlag(0);
			set => Registers.RPF.SetFlag(0, value);
		}

		public bool Overflow
		{
			get => Registers.RPF.GetFlag(1);
			set => Registers.RPF.SetFlag(1, value);
		}

		public ProcessorCore()
		{
			DataBus   = new Register("DATA_BUS");
			Devices   = new Dictionary<int, Device>();
			Registers = new ProcessorRegisters();
			Registers.ResetAll();
		}

#region Core

		private Device CommandDevice => Devices[(int) Registers.RDC.dDWORD];

		private Device MemoryDevice => Devices[(int) Registers.RDR.dDWORD];

		public void Tick()
		{
			//	Шаг времени
			Registers.RTX.QWORD++;

			//	Чтение и исполнение команды
			byte command = CommandDevice.GetValueByte(Registers.RIP.QWORD);

			if (command == 0xFF)
			{
				Console.WriteLine("Command: Complete!");
				Completed = true;
				return;
			}
			
			Console.Write($"{Registers.RIP.QWORD:X8} : ");
			Registers.RIP.QWORD++;
			var cat  = command >> 5 & 0b111;
			var inst = command >> 2 & 0b111;
			var size = command & 0b11;

			switch (cat)
			{
				case 0b000:
					CategoryNop(command, inst, size);
					break;
				case 0b100:
					CategoryBase(command, inst, size);
					break;
				case 0b010:
					CategoryStack(command, inst, size);
					break;
				case 0b110:
					CategoryAddress(command, inst, size);
					break;
				case 0b001:
					CategoryBinary(command, inst, size);
					break;
				case 0b101:
					CategoryMath(command, inst, size);
					break;
				case 0b011:
					CategoryShort(command, inst, size);
					break;
			}
		}

#endregion


#region Instructions

		private void CategoryNop(byte command, int inst, int size)
		{
			//	All NOP
			Console.WriteLine("    NOP");
		}

		private void CategoryBase(byte command, int inst, int size)
		{
			var argument = CommandDevice.GetValueByte(Registers.RIP.QWORD);
			Registers.RIP.QWORD++;
			var from    = (argument >> 4) & 0xF;
			var into    = (argument >> 0) & 0xF;
			var regFrom = Registers[from];
			var regInto = Registers[into];

			switch (inst)
			{
				case 0b000: //100 000 00  MOV reg, reg
					Console.WriteLine($"    MOV {regFrom.Name}, {regInto.Name}");
					Register.Move(regFrom, regInto, size);
					break;
				case 0b001: //100 001 00  MOV reg, [reg]
				{
					Console.WriteLine($"    MOV {regFrom.Name}, [{regInto.Name}]");
					MemoryDevice.GetValue(regFrom.QWORD, regInto, size);
					break;
				}

				case 0b010: //100 010 00  MOV [reg], reg
				{
					Console.WriteLine($"    MOV [{regFrom.Name}], {regInto.Name}");
					MemoryDevice.SetValue(regInto.QWORD, regFrom, size);
					break;
				}

				case 0b011: //100 011 00  MOV [reg], [reg]
				{
					Console.WriteLine($"    MOV [{regFrom.Name}], [{regInto.Name}]");
					//	На самом деле все переносы -из- и -в- регистры идут через внутреннюю шину процессора
					//	Более того, я на 100% уверен что таких шин внутри несколько
					DataBus.QWORD = 0;
					MemoryDevice.GetValue(regFrom.QWORD, DataBus, size);
					MemoryDevice.SetValue(regInto.QWORD, DataBus, size);
					break;
				}

				case 0b100: //100 100 00  MOV const, reg
				{
					CommandDevice.GetValue(Registers.RIP.QWORD, regInto, size);
					Registers.RIP.QWORD += Size.SizeToStep[size];
					Console.WriteLine($"    MOV 0x{regInto.QWORD:X}, {regInto.Name}");
					break;
				}

				case 0b101: //100 101 00  MOV const, [reg]
				{
					DataBus.QWORD = 0;
					CommandDevice.GetValue(Registers.RIP.QWORD, DataBus, size);
					Registers.RIP.QWORD += Size.SizeToStep[size];
					MemoryDevice.SetValue(regInto.QWORD, DataBus, size);
					Console.WriteLine($"    MOV 0x{regInto.QWORD:X}, [{regInto.Name}]");
					break;
				}

				case 0b110: //100 110 00  MOV reg, [const]
				{
					DataBus.QWORD = 0;
					CommandDevice.GetValue(Registers.RIP.QWORD, DataBus, size);
					Registers.RIP.QWORD += Size.SizeToStep[size];
					MemoryDevice.SetValue(DataBus.QWORD, regFrom, size);
					Console.WriteLine($"    MOV {regFrom.Name}, [0x{DataBus.QWORD:X}]");
					break;
				}

				case 0b111: //100 111 00  MOV [const], reg
				{
					DataBus.QWORD = 0;
					CommandDevice.GetValue(Registers.RIP.QWORD, DataBus, size);
					Registers.RIP.QWORD += Size.SizeToStep[size];
					MemoryDevice.GetValue(DataBus.QWORD, regInto, size);
					Console.WriteLine($"    MOV [0x{DataBus.QWORD:X}], {regInto.Name}");
					break;
				}
			}
		}

		private void CategoryStack(byte command, int inst, int size)
		{
			var argument = CommandDevice.GetValueByte(Registers.RIP.QWORD);
			Registers.RIP.QWORD++;
			var stack    = (argument >> 4) & 0xF;
			var data     = (argument >> 0) & 0xF;
			var regStack = Registers[stack];
			var regData  = Registers[data];

			switch (inst)
			{
				case 0b000: // 010 000 00  PUSH reg
				{
					Console.WriteLine("    PUSH reg");
					regStack = Registers.RS0;
					MemoryDevice.SetValue(regStack.QWORD, regData, size);
					regStack.QWORD += Size.SizeToStep[size];
					break;
				}

				case 0b001: // 010 001 00  POP  reg
				{
					Console.WriteLine("    POP reg");
					regStack = Registers.RS0;
					MemoryDevice.GetValue(regStack.QWORD, regData, size);
					regStack.QWORD -= Size.SizeToStep[size];
					break;
				}

				case 0b010: // 010 010 00  PUSH reg, <reg>
				{
					Console.WriteLine("    PUSH reg, <reg>");
					MemoryDevice.SetValue(regStack.QWORD, regData, size);
					regStack.QWORD += Size.SizeToStep[size];
					break;
				}

				case 0b011: // 010 011 00  POP  reg, <reg>
				{
					Console.WriteLine("    POP reg, <reg>");
					MemoryDevice.GetValue(regStack.QWORD, regData, size);
					regStack.QWORD -= Size.SizeToStep[size];
					break;
				}

				case 0b100:
					Interrupt = true;
					break;
				case 0b101:
					Interrupt = true;
					break;
				case 0b110:
					Interrupt = true;
					break;
				case 0b111:
					Interrupt = true;
					break;
			}
		}

		private void Jump(Register address, int size)
		{
			switch (size)
			{
				case Size.Byte:
				{
					var jump = (sbyte) address.dBYTE;
					Registers.RIP.Value += jump;
					break;
				}

				case Size.Word:
				{
					var jump = (short) address.dWORD;
					Registers.RIP.Value += jump;
					break;
				}

				case Size.DWord:
				{
					var jump = (int) address.dDWORD;
					Registers.RIP.Value += jump;
					break;
				}

				case Size.QWord:
				{
					Registers.RIP.QWORD = address.QWORD;
					break;
				}
			}
		}

		private void CategoryAddress(byte command, int inst, int size)
		{
			var argument = CommandDevice.GetValueByte(Registers.RIP.QWORD);
			Registers.RIP.QWORD++;
			var lReg     = (argument >> 4) & 0xF;
			var rReg     = (argument >> 0) & 0xF;
			var regLeft  = Registers[lReg];
			var regRight = Registers[rReg];

			switch (inst)
			{
				case 0b000: // 110 000 00  JMP const
				{
					DataBus.QWORD = 0;
					CommandDevice.GetValue(Registers.RIP.QWORD, DataBus, size);
					//	У нас прыжок считается от позиции начала инструкции
					//	Так что надо вычесть ещё 2 на саму инструкцию, иначе мы никуда не приедем!
					Registers.RIP.QWORD -= 2;
					Jump(DataBus, size);
					Console.WriteLine($"    JMP 0x{DataBus.QWORD:X}");
					break;
				}

				case 0b001: // 110 001 00  JMP reg
				{
					Registers.RIP.QWORD -= 2;
					Jump(regRight, size);
					Console.WriteLine($"    JMP {regRight.Name}");
					break;
				}

				case 0b010: // 110 010 00  -undefined-
				{
					Interrupt = true;
					break;
				}

				case 0b011: // 110 011 00  -undefined-
				{
					Interrupt = true;
					break;
				}

				case 0b100: // 110 100 00  JZ reg, const
				{
					DataBus.QWORD = 0;
					CommandDevice.GetValue(Registers.RIP.QWORD, DataBus, size);
					Registers.RIP.QWORD += Size.SizeToStep[size];
					if (regLeft.QWORD == 0)
					{
						//	В условном переходе ещё сложнее
						Registers.RIP.QWORD -= Size.SizeToStep[size];
						Registers.RIP.QWORD -= 2;
						Jump(DataBus, size);
					}
					Console.WriteLine($"    JZ {regLeft.Name}, 0x{DataBus.QWORD:X}");
					break;
				}

				case 0b101: // 110 101 00  JZ reg, reg
				{
					if (regLeft.QWORD == 0)
					{
						Registers.RIP.QWORD -= 2;
						Jump(regRight, size);
					}
					Console.WriteLine($"    JZ {regLeft.Name}, {regRight.Name}");
					break;
				}

				case 0b110: // 110 110 00  JNZ reg, const
				{
					DataBus.QWORD = 0;
					CommandDevice.GetValue(Registers.RIP.QWORD, DataBus, size);
					Registers.RIP.QWORD += Size.SizeToStep[size];
					if (regLeft.QWORD != 0)
					{
						Registers.RIP.QWORD -= Size.SizeToStep[size];
						Registers.RIP.QWORD -= 2;
						Jump(DataBus, size);
					}
					Console.WriteLine($"    JNZ {regLeft.Name}, 0x{DataBus.QWORD:X}");
					break;
				}

				case 0b111: // 110 111 00  JNZ reg, reg
				{
					Console.WriteLine("    JNZ reg, reg");
					if (regLeft.QWORD != 0)
					{
						Registers.RIP.QWORD -= 2;
						Jump(regRight, size);
					}
					Console.WriteLine($"    JNZ {regLeft.Name}, {regRight.Name}");
					break;
				}
			}
		}

		private void CategoryBinary(byte command, int inst, int size)
		{
			var argument = CommandDevice.GetValueByte(Registers.RIP.QWORD);
			Registers.RIP.QWORD++;
			var lReg     = (argument >> 4) & 0xF;
			var rReg     = (argument >> 0) & 0xF;
			var regLeft  = Registers[lReg];
			var regRight = Registers[rReg];

			switch (inst)
			{
				case 0b000: // 001 000 00  NOT reg
				{
					Console.WriteLine("    NOT reg");
					//	Тут очень удачно пришёлся DataBus
					//	В нём можно посчитать и потом скинуть результат куда надо!
					DataBus.QWORD = ~regRight.QWORD;
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b001: // 001 001 00  AND reg, reg
				{
					Console.WriteLine("    AND reg, reg");
					DataBus.QWORD = regLeft.QWORD & regRight.QWORD;
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b010: // 001 010 00  OR  reg, reg
				{
					Console.WriteLine("    OR reg, reg");
					DataBus.QWORD = regLeft.QWORD | regRight.QWORD;
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b011: // 001 011 00  XOR reg, reg
				{
					Console.WriteLine("    XOR reg, reg");
					DataBus.QWORD = regLeft.QWORD ^ regRight.QWORD;
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b100: // 001 100 00  -undefined-
				{
					Interrupt = true;
					break;
				}

				case 0b101: // 001 101 00  -undefined-
				{
					Interrupt = true;
					break;
				}

				case 0b110: // 001 110 00  -undefined-
				{
					Interrupt = true;
					break;
				}

				case 0b111: // 001 111 00  -undefined-
				{
					Interrupt = true;
					break;
				}
			}
		}

		private void CategoryMath(byte command, int inst, int size)
		{
			var argument = CommandDevice.GetValueByte(Registers.RIP.QWORD);
			Registers.RIP.QWORD++;
			var lReg     = (argument >> 4) & 0xF;
			var rReg     = (argument >> 0) & 0xF;
			var regLeft  = Registers[lReg];
			var regRight = Registers[rReg];

			Overflow = false;
			var lValue = (BigInteger) regLeft.QWORD;
			var rValue = (BigInteger) regRight.QWORD;
			
			switch (inst)
			{
				case 0b000: // 101 000 00  ADD reg, reg
				{
					Console.WriteLine("    ADD reg, reg");
					var result = lValue + rValue;
					DataBus.QWORD = (ulong) result;
					Overflow      = 0 < (result >> (int) (Size.SizeToStep[size] * 8));
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b001: // 101 001 00  SUB reg, reg
				{
					Console.WriteLine("    SUB reg, reg");
					var result = lValue - rValue;
					DataBus.QWORD = (ulong) result;
					Overflow      = 0 < (result >> (int) (Size.SizeToStep[size] * 8));
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b010: // 101 010 00  MUL reg, reg
				{
					Console.WriteLine("    MUL reg, reg");
					var result = lValue * rValue;
					DataBus.QWORD = (ulong) result;
					Overflow      = 0 < (result >> (int) (Size.SizeToStep[size] * 8));
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b011: // 101 011 00  DIV reg, reg
				{
					Console.WriteLine("    DIV reg, reg");
					var result = lValue / rValue;
					DataBus.QWORD = (ulong) result;
					Overflow      = 0 < (result >> (int) (Size.SizeToStep[size] * 8));
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b100: // 101 100 00  MOD reg, reg
				{
					Console.WriteLine("    MOD reg, reg");
					var result = lValue % rValue;
					DataBus.QWORD = (ulong) result;
					Overflow      = 0 < (result >> (int) (Size.SizeToStep[size] * 8));
					Register.Move(DataBus, regLeft, size);
					break;
				}

				case 0b101: // 001 101 00  -undefined-
				{
					Interrupt = true;
					break;
				}

				case 0b110: // 001 110 00  -undefined-
				{
					Interrupt = true;
					break;
				}

				case 0b111: // 001 111 00  -undefined-
				{
					Interrupt = true;
					break;
				}
			}
		}

		private void CategoryShort(byte command, int inst, int size)
		{
			var mod      = (command >> 4) & 0b1;
			var reg      = (command >> 0) & 0b1111;
			var register = Registers[reg];

			if (mod == 0)
			{
				//011 0 0000  INC reg
				Console.WriteLine("    INC reg");
				register.Inc();
			}
			else
			{
				//011 1 0000  DEC reg
				Console.WriteLine("    DEC reg");
				register.Dec();
			}
		}

#endregion
	}
}
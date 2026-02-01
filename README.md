# RISC-V Assembler & Single-Cycle Simulator

## Overview

This project is a C# library designed to assemble RISC-V assembly code into machine code and simulate its execution on a software-defined Single-Cycle processor. It consists of three main components: a core utility library (`LibUtils`), a CPU simulation engine (`LibCPU`), and an assembler (`Assembler`).

## Project Structure

The solution is organized into three directories, each containing a specific module:

### 1. `LibUtils/` (Core Utilities)

Contains `LibUtils.cs`, which provides shared definitions and helper functions used by both the assembler and the CPU simulator.

* **Register Definitions**: Maps ABI names (e.g., `sp`, `ra`, `a0`) to their 5-bit binary representations.
* **Instruction Info**: Stores opcode, funct3, and funct7 values for supported RV32/RV64 instructions.
* **Data Structures**: Defines `Instruction`, `Program`, and `InstInfo` structs.
* **Helpers**: Includes methods for binary conversion (`StringToBin`, `LongToBin`), sign extension (`sext`), and memory initialization file generation (`GetIMMIF`, `GetDMMIF`).

### 2. `Assembler/` (The Assembler)

Contains `Assembler.cs`, responsible for parsing assembly source files and generating machine code.

* **Two-Pass Assembly**:
1. **Parsing & Label Resolution**: Scans the text and data sections, resolves addresses for labels and data directives (`.word`, `.string`, `.space`).
2. **Code Generation**: Converts parsed tokens into 32-bit machine code instructions.


* **Pseudo-Instruction Support**: Expands complex instructions like `la` (load address), `li` (load immediate), `call`, and `j` into their base RISC-V instructions (e.g., `auipc` + `addi`).
* **Directives**: Supports `.section .text`, `.section .data`, `.globl`, and data allocation directives.

### 3. `LibCPU/` (The Simulator)

Contains `LibCPU.cs`, which implements the Single-Cycle RISC-V processor logic.

* **Register File**: A 64-bit register file (RV64) simulation.
* **Memory Model**: A byte-addressable memory system used for both instruction and data memory.
* **Execution Loop**: Fetches, decodes, and executes instructions in a single cycle.
* **System Calls (`ecall`)**:
* `print_string` (when `a7=64`, `a0=1`): Prints a string from memory to the console.
* `exit` (when `a7=93`): Terminates the simulation with the exit code in `a0`.



## Features

* **ISA Support**: Implements a subset of the **RV64I** base integer instruction set (R-Type, I-Type, S-Type, U-Type, and B-Type branching).
* **Pseudo-Instructions**: Handles `mv`, `nop`, `not`, `neg`, `li`, `la`, `call`, `ret`, `j`, `jr`, etc.
* **Memory Output**: Can generate Altera/Intel Memory Initialization Files (`.mif`) and Verilog initialization blocks for FPGA synthesis.
* **Trace Logging**: Option to log parsed instruction details (opcode, funct3/7, operands) for debugging.
* **State Dumping**: Upon exit, the simulator can write the final state of the Register File and Data Memory to a file.

## Usage

### Assembling a Program

To assemble a source file (e.g., `test.s`) and get the program structure:

```csharp
using Assembler;

// Assemble the program (second argument enables verbose instruction logging)
LibUtils.Program prog = Assembler.AssembleProgram("path/to/test.s", false);

// Access machine codes
List<string> machineCodes = prog.MachineCodes;

```

### Running the Simulator

To execute the assembled program:

```csharp
using LibCPU;

// Initialize parameters
uint IM_SIZE = 1024; // Instruction Memory Size
uint DM_SIZE = 4096; // Data Memory Size
List<string> cmdArgs = new List<string> { "arg1", "arg2" }; // Command line args for the simulated program

// Run the simulation
SingleCycle.Run(
    prog.MachineCodes, 
    prog.DataMemoryValues, 
    IM_SIZE, 
    DM_SIZE, 
    cmdArgs, 
    "output_state.txt" // Optional: File path to dump CPU state upon exit
);

```

## Supported Assembly Syntax

**Data Section:**

```assembly
.section .data
msg:    .string "Hello World\n"
val:    .word 42
buffer: .space 100

```

**Text Section:**

```assembly
.section .text
.globl main
main:
    la a0, msg      # Load address of string
    li a7, 64       # Syscall for write
    li a0, 1        # File descriptor (stdout)
    li a2, 12       # Length
    ecall           # Print string

    li a7, 93       # Syscall for exit
    li a0, 0        # Exit code 0
    ecall

```

## Dependencies

* .NET 6.0 or later (implied by file-scoped namespaces and C# features).

---

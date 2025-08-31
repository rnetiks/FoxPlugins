using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IL2CSharpDecompiler
{
    
    public class ILInstruction
    {
        public string Label { get; set; }
        public string OpCode { get; set; }
        public string Operand { get; set; }
        public int Offset { get; set; }
        
        public ILInstruction(string label, string opCode, string operand = null)
        {
            Label = label;
            OpCode = opCode.ToLower();
            Operand = operand;
            if (label != null)
            {
                var match = Regex.Match(label, @"IL_([0-9A-Fa-f]+)");
                if (match.Success)
                    Offset = Convert.ToInt32(match.Groups[1].Value, 16);
            }
        }
    }

    
    public class StackValue
    {
        public string Expression { get; set; }
        public string Type { get; set; }
        public bool IsFieldAddress { get; set; }
        
        public StackValue(string expr, string type = null, bool isFieldAddr = false)
        {
            Expression = expr;
            Type = type;
            IsFieldAddress = isFieldAddr;
        }
    }

    
    public class BasicBlock
    {
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public List<ILInstruction> Instructions { get; set; } = new List<ILInstruction>();
        public List<BasicBlock> Successors { get; set; } = new List<BasicBlock>();
        public List<BasicBlock> Predecessors { get; set; } = new List<BasicBlock>();
        public bool IsLoopHeader { get; set; }
        public bool IsLoopEnd { get; set; }
        public BasicBlock LoopHeader { get; set; }
        public bool IsTryBlock { get; set; }
        public bool IsFinallyBlock { get; set; }
        public BasicBlock FinallyBlock { get; set; }
        public bool IsSwitchBlock { get; set; }
        public List<int> SwitchTargets { get; set; }
        public string Label { get; set; }
    }

    
    public class ILDecompiler
    {
        private List<ILInstruction> instructions;
        private Dictionary<int, BasicBlock> blocks;
        private List<string> localVariables;
        private List<string> parameters;
        private string returnType;
        private string methodName;
        private StringBuilder output;
        private int indentLevel;
        private Dictionary<int, string> labelTargets;
        private Stack<StackValue> evaluationStack;
        private Dictionary<int, List<string>> statements;
        private Dictionary<int, string> localVarNames;

        public string Decompile(string ilCode, string methodName = "Method", 
                               string returnType = "void", 
                               List<string> parameters = null,
                               List<string> localVarTypes = null)
        {
            this.methodName = methodName;
            this.returnType = returnType;
            this.parameters = parameters ?? new List<string>();
            localVariables = localVarTypes ?? new List<string>();
            
            Initialize();
            ParseInstructions(ilCode);
            AnalyzeControlFlow();
            GenerateCSharpCode();
            
            return output.ToString();
        }
        
        public string Decompile(ILInstruction[] ilInstructions, string methodName = "Method", 
                               string returnType = "void", 
                               List<string> parameters = null,
                               List<string> localVarTypes = null)
        {
            this.methodName = methodName;
            this.returnType = returnType;
            this.parameters = parameters ?? new List<string>();
            localVariables = localVarTypes ?? new List<string>();
            
            Initialize();
            instructions = new List<ILInstruction>(ilInstructions);
            AnalyzeControlFlow();
            GenerateCSharpCode();
            
            return output.ToString();
        }
        
        public string Decompile(List<ILInstruction> ilInstructions, string methodName = "Method", 
                               string returnType = "void", 
                               List<string> parameters = null,
                               List<string> localVarTypes = null)
        {
            this.methodName = methodName;
            this.returnType = returnType;
            this.parameters = parameters ?? new List<string>();
            localVariables = localVarTypes ?? new List<string>();
            
            Initialize();
            instructions = new List<ILInstruction>(ilInstructions);
            AnalyzeControlFlow();
            GenerateCSharpCode();
            
            return output.ToString();
        }

        private void Initialize()
        {
            instructions = new List<ILInstruction>();
            blocks = new Dictionary<int, BasicBlock>();
            output = new StringBuilder();
            indentLevel = 0;
            labelTargets = new Dictionary<int, string>();
            evaluationStack = new Stack<StackValue>();
            statements = new Dictionary<int, List<string>>();
            localVarNames = new Dictionary<int, string>();
            
            
            for (int i = 0; i < localVariables.Count; i++)
            {
                localVarNames[i] = GenerateVarName(localVariables[i], i);
            }
        }

        private string GenerateVarName(string type, int index)
        {
            var typeName = type.Split('.').Last().ToLower();
            switch (typeName)
            {
                case "string": return $"str{index}";
                case "int32": return $"num{index}";
                case "boolean": return $"flag{index}";
                default: return $"var{index}";
            }
        }

        private void ParseInstructions(string ilCode)
        {
            var lines = ilCode.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                
                var match = Regex.Match(trimmed, @"^(IL_[0-9A-Fa-f]+):\s+(\S+)(?:\s+(.+))?$");
                if (match.Success)
                {
                    var label = match.Groups[1].Value;
                    var opCode = match.Groups[2].Value;
                    var operand = match.Groups[3].Success ? match.Groups[3].Value : null;
                    
                    instructions.Add(new ILInstruction(label, opCode, operand));
                }
            }
        }

        private void AnalyzeControlFlow()
        {
            
            var blockStarts = new HashSet<int> { 0 };
            
            for (int i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];
                
                
                if (IsBranchInstruction(inst.OpCode))
                {
                    var target = GetBranchTarget(inst);
                    if (target >= 0)
                    {
                        blockStarts.Add(target);
                        if (i + 1 < instructions.Count && !IsUnconditionalBranch(inst.OpCode))
                            blockStarts.Add(i + 1);
                    }
                }
                
                
                if (inst.OpCode == "ret" || inst.OpCode == "leave" || 
                    inst.OpCode == "leave.s" || inst.OpCode == "endfinally" ||
                    inst.OpCode == "throw" || inst.OpCode == "rethrow" ||
                    inst.OpCode == "br" || inst.OpCode == "br.s")
                {
                    if (i + 1 < instructions.Count)
                        blockStarts.Add(i + 1);
                }
                
                
                if (inst.OpCode == "switch")
                {
                    var targets = ParseSwitchTargets(inst.Operand);
                    foreach (var t in targets)
                    {
                        blockStarts.Add(t);
                    }
                    if (i + 1 < instructions.Count)
                        blockStarts.Add(i + 1);
                }
            }
            
            
            var sortedStarts = blockStarts.OrderBy(x => x).ToList();
            for (int i = 0; i < sortedStarts.Count; i++)
            {
                var block = new BasicBlock
                {
                    StartOffset = sortedStarts[i],
                    EndOffset = i + 1 < sortedStarts.Count ? sortedStarts[i + 1] - 1 : instructions.Count - 1,
                    Label = $"label_{sortedStarts[i]:X4}"
                };
                
                for (int j = block.StartOffset; j <= block.EndOffset; j++)
                {
                    block.Instructions.Add(instructions[j]);
                }
                
                blocks[block.StartOffset] = block;
            }
            
            
            BuildControlFlowGraph();
            
            
            IdentifyLoops();
            
            
            IdentifySwitchBlocks();
            
            
            IdentifyExceptionBlocks();
        }
        
        private void BuildControlFlowGraph()
        {
            foreach (var block in blocks.Values)
            {
                if (block.Instructions.Count == 0) continue;
                
                var lastInst = block.Instructions.Last();
                
                
                if (IsBranchInstruction(lastInst.OpCode))
                {
                    var targetOffset = GetBranchTarget(lastInst);
                    if (targetOffset >= 0 && blocks.ContainsKey(targetOffset))
                    {
                        var targetBlock = blocks[targetOffset];
                        block.Successors.Add(targetBlock);
                        targetBlock.Predecessors.Add(block);
                    }
                    
                    
                    if (!IsUnconditionalBranch(lastInst.OpCode))
                    {
                        var nextOffset = block.EndOffset + 1;
                        if (nextOffset < instructions.Count)
                        {
                            var nextBlock = blocks.Values.FirstOrDefault(b => b.StartOffset == nextOffset);
                            if (nextBlock != null)
                            {
                                block.Successors.Add(nextBlock);
                                nextBlock.Predecessors.Add(block);
                            }
                        }
                    }
                }
                
                else if (lastInst.OpCode == "switch")
                {
                    var targets = ParseSwitchTargets(lastInst.Operand);
                    block.SwitchTargets = targets;
                    block.IsSwitchBlock = true;
                    
                    foreach (var target in targets)
                    {
                        if (blocks.ContainsKey(target))
                        {
                            var targetBlock = blocks[target];
                            block.Successors.Add(targetBlock);
                            targetBlock.Predecessors.Add(block);
                        }
                    }
                    
                    
                    var defaultOffset = block.EndOffset + 1;
                    if (defaultOffset < instructions.Count)
                    {
                        var defaultBlock = blocks.Values.FirstOrDefault(b => b.StartOffset == defaultOffset);
                        if (defaultBlock != null)
                        {
                            block.Successors.Add(defaultBlock);
                            defaultBlock.Predecessors.Add(block);
                        }
                    }
                }
                
                else if (lastInst.OpCode != "ret" && lastInst.OpCode != "throw" && 
                         lastInst.OpCode != "rethrow" && lastInst.OpCode != "endfinally")
                {
                    var nextOffset = block.EndOffset + 1;
                    if (nextOffset < instructions.Count)
                    {
                        var nextBlock = blocks.Values.FirstOrDefault(b => b.StartOffset == nextOffset);
                        if (nextBlock != null)
                        {
                            block.Successors.Add(nextBlock);
                            nextBlock.Predecessors.Add(block);
                        }
                    }
                }
            }
        }
        
        private void IdentifyLoops()
        {
            
            foreach (var block in blocks.Values)
            {
                if (block.Instructions.Count == 0) continue;
                
                var lastInst = block.Instructions.Last();
                if (IsBranchInstruction(lastInst.OpCode))
                {
                    var targetOffset = GetBranchTarget(lastInst);
                    
                    
                    if (targetOffset >= 0 && targetOffset <= block.StartOffset)
                    {
                        if (blocks.ContainsKey(targetOffset))
                        {
                            var headerBlock = blocks[targetOffset];
                            headerBlock.IsLoopHeader = true;
                            block.IsLoopEnd = true;
                            block.LoopHeader = headerBlock;
                        }
                    }
                }
            }
        }
        
        private void IdentifySwitchBlocks()
        {
            foreach (var block in blocks.Values)
            {
                if (block.Instructions.Count == 0) continue;
                
                var lastInst = block.Instructions.Last();
                if (lastInst.OpCode == "switch")
                {
                    block.IsSwitchBlock = true;
                }
            }
        }
        
        private List<int> ParseSwitchTargets(string operand)
        {
            var targets = new List<int>();
            
            
            if (!string.IsNullOrEmpty(operand))
            {
                var parts = operand.Split(',');
                foreach (var part in parts)
                {
                    if (part.Contains("IL_"))
                    {
                        var match = Regex.Match(part, @"IL_([0-9A-Fa-f]+)");
                        if (match.Success)
                        {
                            targets.Add(Convert.ToInt32(match.Groups[1].Value, 16));
                        }
                    }
                }
            }
            return targets;
        }
        
        private bool IsUnconditionalBranch(string opCode)
        {
            return opCode == "br" || opCode == "br.s" || 
                   opCode == "leave" || opCode == "leave.s";
        }

        private void IdentifyExceptionBlocks()
        {
            
            BasicBlock currentTryBlock = null;
            BasicBlock finallyBlock = null;
            
            foreach (var block in blocks.Values)
            {
                foreach (var inst in block.Instructions)
                {
                    if (inst.OpCode == "leave" || inst.OpCode == "leave.s")
                    {
                        block.IsTryBlock = true;
                        currentTryBlock = block;
                    }
                    else if (inst.OpCode == "endfinally")
                    {
                        block.IsFinallyBlock = true;
                        finallyBlock = block;
                        
                        if (currentTryBlock != null)
                        {
                            currentTryBlock.FinallyBlock = finallyBlock;
                        }
                    }
                }
            }
        }

        private bool IsBranchInstruction(string opCode)
        {
            return opCode.StartsWith("br") || opCode.StartsWith("bne") || 
                   opCode.StartsWith("beq") || opCode.StartsWith("blt") ||
                   opCode.StartsWith("bgt") || opCode.StartsWith("ble") ||
                   opCode.StartsWith("bge") || opCode == "leave" || 
                   opCode == "leave.s";
        }

        private int GetBranchTarget(ILInstruction inst)
        {
            if (inst.Operand == null) return -1;
            
            var targetLabel = inst.Operand.Trim();
            var targetInst = instructions.FirstOrDefault(i => i.Label == targetLabel);
            
            if (targetInst != null)
            {
                return instructions.IndexOf(targetInst);
            }
            
            return -1;
        }

        private void GenerateCSharpCode()
        {
            
            GenerateMethodSignature();
            
            var processedOffsets = new HashSet<int>();
            
            
            var sortedBlocks = blocks.Values.OrderBy(b => b.StartOffset).ToList();
            
            foreach (var block in sortedBlocks)
            {
                if (processedOffsets.Contains(block.StartOffset))
                    continue;
                    
                
                if (block.IsLoopHeader)
                {
                    var loopEnd = FindLoopEnd(block);
                    if (loopEnd != null)
                    {
                        ProcessLoop(block, loopEnd, processedOffsets);
                        continue;
                    }
                }
                
                
                if (block.IsSwitchBlock)
                {
                    ProcessSwitchBlock(block, processedOffsets);
                    continue;
                }
                
                
                ProcessBasicBlock(block);
                processedOffsets.Add(block.StartOffset);
            }
            
            
            indentLevel--;
            WriteLine("}");
        }
        
        private void GenerateMethodSignature()
        {
            
            var paramList = new List<string>();
            for (int i = 0; i < parameters.Count; i++)
            {
                paramList.Add($"{parameters[i]} param{i}");
            }
            
            var paramString = paramList.Count > 0 ? string.Join(", ", paramList.ToArray()) : "";
            
            
            WriteLine($"{returnType} {methodName}({paramString})");
            WriteLine("{");
            indentLevel++;
        }
        
        private BasicBlock FindLoopEnd(BasicBlock loopHeader)
        {
            
            foreach (var block in blocks.Values)
            {
                if (block.IsLoopEnd && block.LoopHeader == loopHeader)
                {
                    return block;
                }
            }
            return null;
        }
        
        private void ProcessLoop(BasicBlock loopHeader, BasicBlock loopEnd, HashSet<int> processedOffsets)
        {
            evaluationStack.Clear();
            
            
            var loopType = AnalyzeLoopType(loopHeader, loopEnd);
            
            switch (loopType)
            {
                case LoopType.For:
                    ProcessForLoop(loopHeader, loopEnd, processedOffsets);
                    break;
                case LoopType.While:
                    ProcessWhileLoop(loopHeader, loopEnd, processedOffsets);
                    break;
                case LoopType.DoWhile:
                    ProcessDoWhileLoop(loopHeader, loopEnd, processedOffsets);
                    break;
                default:
                    
                    ProcessBasicBlock(loopHeader);
                    processedOffsets.Add(loopHeader.StartOffset);
                    break;
            }
        }
        
        private enum LoopType
        {
            Unknown,
            For,
            While,
            DoWhile
        }
        
        private LoopType AnalyzeLoopType(BasicBlock loopHeader, BasicBlock loopEnd)
        {
            
            bool hasInitialization = false;
            bool hasCondition = false;
            bool hasIncrement = false;
            
            
            if (loopHeader.Predecessors.Count > 0)
            {
                var prevBlock = loopHeader.Predecessors.FirstOrDefault(p => p.EndOffset < loopHeader.StartOffset);
                if (prevBlock != null)
                {
                    foreach (var inst in prevBlock.Instructions)
                    {
                        if (inst.OpCode.StartsWith("ldc.i4") || inst.OpCode == "stloc")
                        {
                            hasInitialization = true;
                            break;
                        }
                    }
                }
            }
            
            
            foreach (var inst in loopHeader.Instructions)
            {
                if (inst.OpCode.StartsWith("blt") || inst.OpCode.StartsWith("bgt") || 
                    inst.OpCode.StartsWith("ble") || inst.OpCode.StartsWith("bge"))
                {
                    hasCondition = true;
                    break;
                }
            }
            
            
            foreach (var inst in loopEnd.Instructions)
            {
                if (inst.OpCode == "add" || inst.OpCode == "sub")
                {
                    hasIncrement = true;
                    break;
                }
            }
            
            if (hasInitialization && hasCondition && hasIncrement)
                return LoopType.For;
            else if (hasCondition)
                return LoopType.While;
            else
                return LoopType.Unknown;
        }
        
        private void ProcessForLoop(BasicBlock loopHeader, BasicBlock loopEnd, HashSet<int> processedOffsets)
        {
            
            string loopVar = "i";
            string initialValue = "0";
            string condition = "";
            string increment = "++";
            
            
            foreach (var inst in loopHeader.Instructions)
            {
                if (inst.OpCode == "ldloc.0" || inst.OpCode == "ldloc.1" || 
                    inst.OpCode == "ldloc.2" || inst.OpCode == "ldloc.3")
                {
                    var idx = int.Parse(inst.OpCode.Split('.')[1]);
                    if (localVarNames.ContainsKey(idx))
                    {
                        loopVar = localVarNames[idx];
                        break;
                    }
                }
            }
            
            
            string limit = "limit";
            foreach (var inst in loopHeader.Instructions)
            {
                if (inst.OpCode.StartsWith("blt"))
                {
                    condition = $"{loopVar} < {limit}";
                    break;
                }
                else if (inst.OpCode.StartsWith("ble"))
                {
                    condition = $"{loopVar} <= {limit}";
                    break;
                }
                else if (inst.OpCode.StartsWith("bgt"))
                {
                    condition = $"{loopVar} > {limit}";
                    break;
                }
                else if (inst.OpCode.StartsWith("bge"))
                {
                    condition = $"{loopVar} >= {limit}";
                    break;
                }
            }
            
            if (string.IsNullOrEmpty(condition))
                condition = $"{loopVar} < {limit}";
            
            
            WriteLine($"for (int {loopVar} = {initialValue}; {condition}; {loopVar}{increment})");
            WriteLine("{");
            indentLevel++;
            
            
            var currentBlock = loopHeader.Successors.FirstOrDefault(s => s != loopEnd && s.StartOffset > loopHeader.StartOffset);
            while (currentBlock != null && currentBlock != loopEnd)
            {
                ProcessBasicBlock(currentBlock);
                processedOffsets.Add(currentBlock.StartOffset);
                currentBlock = currentBlock.Successors.FirstOrDefault(s => !processedOffsets.Contains(s.StartOffset) && s != loopEnd);
            }
            
            indentLevel--;
            WriteLine("}");
            
            processedOffsets.Add(loopHeader.StartOffset);
            processedOffsets.Add(loopEnd.StartOffset);
        }
        
        private void ProcessWhileLoop(BasicBlock loopHeader, BasicBlock loopEnd, HashSet<int> processedOffsets)
        {
            evaluationStack.Clear();
            
            
            string condition = "true"; 
            
            
            foreach (var inst in loopHeader.Instructions)
            {
                if (inst.OpCode.StartsWith("blt") || inst.OpCode.StartsWith("ble") ||
                    inst.OpCode.StartsWith("bgt") || inst.OpCode.StartsWith("bge") ||
                    inst.OpCode.StartsWith("beq") || inst.OpCode.StartsWith("bne") ||
                    inst.OpCode.StartsWith("brtrue") || inst.OpCode.StartsWith("brfalse"))
                {
                    
                    var result = ProcessInstruction(inst, false);
                    if (!string.IsNullOrEmpty(result) && result.StartsWith("if ("))
                    {
                        condition = result.Substring(4, result.Length - 5);
                    }
                    break;
                }
                else
                {
                    ProcessInstruction(inst, false);
                }
            }
            
            WriteLine($"while ({condition})");
            WriteLine("{");
            indentLevel++;
            
            
            var currentBlock = loopHeader.Successors.FirstOrDefault(s => s != loopEnd && s.StartOffset > loopHeader.StartOffset);
            while (currentBlock != null && currentBlock != loopEnd)
            {
                ProcessBasicBlock(currentBlock);
                processedOffsets.Add(currentBlock.StartOffset);
                currentBlock = currentBlock.Successors.FirstOrDefault(s => !processedOffsets.Contains(s.StartOffset) && s != loopEnd);
            }
            
            indentLevel--;
            WriteLine("}");
            
            processedOffsets.Add(loopHeader.StartOffset);
            processedOffsets.Add(loopEnd.StartOffset);
        }
        
        private void ProcessDoWhileLoop(BasicBlock loopHeader, BasicBlock loopEnd, HashSet<int> processedOffsets)
        {
            WriteLine("do");
            WriteLine("{");
            indentLevel++;
            
            
            ProcessBasicBlock(loopHeader);
            processedOffsets.Add(loopHeader.StartOffset);
            
            var currentBlock = loopHeader.Successors.FirstOrDefault(s => s != loopEnd);
            while (currentBlock != null && currentBlock != loopEnd)
            {
                ProcessBasicBlock(currentBlock);
                processedOffsets.Add(currentBlock.StartOffset);
                currentBlock = currentBlock.Successors.FirstOrDefault(s => !processedOffsets.Contains(s.StartOffset) && s != loopEnd);
            }
            
            indentLevel--;
            
            
            string condition = "true";
            evaluationStack.Clear();
            
            foreach (var inst in loopEnd.Instructions)
            {
                if (inst.OpCode.StartsWith("brtrue") || inst.OpCode.StartsWith("brfalse") ||
                    inst.OpCode.StartsWith("beq") || inst.OpCode.StartsWith("bne") ||
                    inst.OpCode.StartsWith("blt") || inst.OpCode.StartsWith("bgt"))
                {
                    var result = ProcessInstruction(inst, false);
                    if (!string.IsNullOrEmpty(result) && result.StartsWith("if ("))
                    {
                        condition = result.Substring(4, result.Length - 5);
                        
                        if (inst.OpCode.StartsWith("brfalse"))
                        {
                            condition = $"!({condition})";
                        }
                    }
                    break;
                }
                ProcessInstruction(inst, false);
            }
            
            WriteLine($"}} while ({condition});");
            processedOffsets.Add(loopEnd.StartOffset);
        }
        
        private void ProcessSwitchBlock(BasicBlock switchBlock, HashSet<int> processedOffsets)
        {
            evaluationStack.Clear();
            
            
            string switchExpression = "value";
            foreach (var inst in switchBlock.Instructions)
            {
                if (inst.OpCode != "switch")
                {
                    ProcessInstruction(inst, false);
                }
                else
                {
                    if (evaluationStack.Count > 0)
                    {
                        switchExpression = evaluationStack.Pop().Expression;
                    }
                    break;
                }
            }
            
            WriteLine($"switch ({switchExpression})");
            WriteLine("{");
            indentLevel++;
            
            
            if (switchBlock.SwitchTargets != null)
            {
                for (int i = 0; i < switchBlock.SwitchTargets.Count; i++)
                {
                    WriteLine($"case {i}:");
                    indentLevel++;
                    
                    var targetOffset = switchBlock.SwitchTargets[i];
                    if (blocks.ContainsKey(targetOffset))
                    {
                        ProcessBasicBlock(blocks[targetOffset]);
                        processedOffsets.Add(targetOffset);
                    }
                    
                    WriteLine("break;");
                    indentLevel--;
                }
            }
            
            
            WriteLine("default:");
            indentLevel++;
            var defaultBlock = switchBlock.Successors.LastOrDefault();
            if (defaultBlock != null && !processedOffsets.Contains(defaultBlock.StartOffset))
            {
                ProcessBasicBlock(defaultBlock);
                processedOffsets.Add(defaultBlock.StartOffset);
            }
            WriteLine("break;");
            indentLevel--;
            
            indentLevel--;
            WriteLine("}");
            
            processedOffsets.Add(switchBlock.StartOffset);
        }
        private void ProcessBasicBlock(BasicBlock block)
        {
            evaluationStack.Clear();
            var blockStatements = new List<string>();
            BasicBlock finallyBlock = null;
            bool inTryBlock = false;

            for (int i = 0; i < block.Instructions.Count; i++)
            {
                var inst = block.Instructions[i];
                var statement = ProcessInstruction(inst, i == block.Instructions.Count - 1);
                
                if (!string.IsNullOrEmpty(statement))
                {
                    
                    if (statement.StartsWith("if (") && !statement.EndsWith(";"))
                    {
                        WriteLine(statement);
                        WriteLine("{");
                        indentLevel++;
                        continue;
                    }
                    else if (statement.StartsWith("for (") || statement.StartsWith("while (") || 
                             statement.StartsWith("switch (") || statement == "do")
                    {
                        WriteLine(statement);
                        if (statement != "do")
                        {
                            WriteLine("{");
                            indentLevel++;
                        }
                        continue;
                    }
                    else if (statement.StartsWith("goto "))
                    {
                        
                        WriteLine(statement);
                        continue;
                    }
                    
                    blockStatements.Add(statement);
                }
                
                
                if (inst.OpCode == "leave" || inst.OpCode == "leave.s")
                {
                    inTryBlock = true;
                    var targetOffset = GetBranchTarget(inst);
                    if (targetOffset >= 0)
                    {
                        
                        foreach (var b in blocks.Values)
                        {
                            if (b.IsFinallyBlock && b.StartOffset < targetOffset)
                            {
                                finallyBlock = b;
                                break;
                            }
                        }
                    }
                }
                
                
                if (inst.OpCode == "ret" && i == block.Instructions.Count - 1)
                {
                    
                    foreach (var stmt in blockStatements)
                    {
                        if (!IsResourceDeclaration(stmt))
                            WriteLine(stmt);
                    }
                    blockStatements.Clear();
                    
                    
                    if (block.Predecessors.Any(p => p.Instructions.LastOrDefault()?.OpCode?.StartsWith("br") == true))
                    {
                        
                        if (indentLevel > 1)
                        {
                            indentLevel--;
                            WriteLine("}");
                        }
                    }
                }
            }

            
            if (inTryBlock && finallyBlock != null)
            {
                
                var usingStatements = ExtractUsingStatements(blockStatements);
                foreach (var usingStmt in usingStatements)
                {
                    WriteLine($"using ({usingStmt})");
                    WriteLine("{");
                    indentLevel++;
                }
                
                
                foreach (var stmt in blockStatements.Where(s => !IsResourceDeclaration(s)))
                {
                    WriteLine(stmt);
                }
                
                
                for (int i = 0; i < usingStatements.Count; i++)
                {
                    indentLevel--;
                    WriteLine("}");
                }
            }
            else
            {
                
                foreach (var stmt in blockStatements)
                {
                    WriteLine(stmt);
                }
            }
            
            
            if (block.Instructions.Count > 0)
            {
                var lastInst = block.Instructions.Last();
                
                
                if ((lastInst.OpCode == "br" || lastInst.OpCode == "br.s") && indentLevel > 1)
                {
                    var target = GetBranchTarget(lastInst);
                    
                    if (target > block.EndOffset + 1)
                    {
                        indentLevel--;
                        WriteLine("}");
                    }
                }
                
                
                if (block.Predecessors.Any(p => 
                    p.Instructions.LastOrDefault()?.OpCode == "br" || 
                    p.Instructions.LastOrDefault()?.OpCode == "br.s"))
                {
                    
                    if (labelTargets.ContainsValue(block.Label))
                    {
                        output.Insert(output.Length - blockStatements.Count * 50, $"{block.Label}:\n");
                    }
                }
            }
        }

        private string ProcessInstruction(ILInstruction inst, bool isLastInBlock)
        {
            switch (inst.OpCode)
            {
                case "nop":
                    return null;
                    
                case "ret":
                    if (evaluationStack.Count > 0 && returnType != "void")
                    {
                        var retVal = evaluationStack.Pop();
                        return $"return {retVal.Expression};";
                    }
                    return "return;";
                    
                case "ldarg.0":
                    evaluationStack.Push(new StackValue("this"));
                    return null;
                
                case "ldarg.1":
                case "ldarg.2":
                case "ldarg.3":
                    var argIndex = int.Parse(inst.OpCode.Split('.')[1]) - 1;
                    evaluationStack.Push(new StackValue($"arg{argIndex}"));
                    return null;
                    
                case "ldarg":
                case "ldarg.s":
                    var argIdx = int.Parse(inst.Operand);
                    evaluationStack.Push(new StackValue(argIdx == 0 ? "this" : $"arg{argIdx - 1}"));
                    return null;
                    
                case "ldloc.0":
                case "ldloc.1":
                case "ldloc.2":
                case "ldloc.3":
                    var locIndex = int.Parse(inst.OpCode.Split('.')[1]);
                    evaluationStack.Push(new StackValue(localVarNames[locIndex]));
                    return null;
                    
                case "ldloc.s":
                case "ldloc":
                    var locIdx = int.Parse(inst.Operand);
                    evaluationStack.Push(new StackValue(localVarNames[locIdx]));
                    return null;
                    
                case "ldloca.s":
                case "ldloca":
                    var locAddrIdx = int.Parse(inst.Operand);
                    evaluationStack.Push(new StackValue($"ref {localVarNames[locAddrIdx]}"));
                    return null;
                    
                case "stloc.0":
                case "stloc.1":
                case "stloc.2":
                case "stloc.3":
                    var stlocIndex = int.Parse(inst.OpCode.Split('.')[1]);
                    if (evaluationStack.Count > 0)
                    {
                        var value = evaluationStack.Pop();
                        var varName = localVarNames[stlocIndex];
                        var varType = localVariables[stlocIndex];
                        
                        
                        if (value.Expression.StartsWith("new "))
                        {
                            return $"{varType} {varName} = {value.Expression};";
                        }
                        return $"{varName} = {value.Expression};";
                    }
                    return null;
                    
                case "stloc.s":
                case "stloc":
                    var stlocIdx = int.Parse(inst.Operand);
                    if (evaluationStack.Count > 0)
                    {
                        var value = evaluationStack.Pop();
                        var varName = localVarNames[stlocIdx];
                        var varType = localVariables[stlocIdx];
                        
                        if (value.Expression.StartsWith("new "))
                        {
                            return $"{varType} {varName} = {value.Expression};";
                        }
                        return $"{varName} = {value.Expression};";
                    }
                    return null;
                    
                case "ldstr":
                    var str = inst.Operand.Trim('"');
                    evaluationStack.Push(new StackValue($"\"{str}\""));
                    return null;
                    
                case "ldnull":
                    evaluationStack.Push(new StackValue("null"));
                    return null;
                    
                case "ldc.i4.0":
                    evaluationStack.Push(new StackValue("0"));
                    return null;
                    
                case "ldc.i4.1":
                    evaluationStack.Push(new StackValue("1"));
                    return null;
                    
                case "ldc.i4.2":
                    evaluationStack.Push(new StackValue("2"));
                    return null;
                    
                case "ldc.i4.3":
                    evaluationStack.Push(new StackValue("3"));
                    return null;
                    
                case "ldc.i4.4":
                    evaluationStack.Push(new StackValue("4"));
                    return null;
                    
                case "ldc.i4.5":
                    evaluationStack.Push(new StackValue("5"));
                    return null;
                    
                case "ldc.i4.6":
                    evaluationStack.Push(new StackValue("6"));
                    return null;
                    
                case "ldc.i4.7":
                    evaluationStack.Push(new StackValue("7"));
                    return null;
                    
                case "ldc.i4.8":
                    evaluationStack.Push(new StackValue("8"));
                    return null;
                    
                case "ldc.i4.m1":
                    evaluationStack.Push(new StackValue("-1"));
                    return null;
                    
                case "ldc.i4":
                case "ldc.i4.s":
                    evaluationStack.Push(new StackValue(inst.Operand));
                    return null;
                    
                case "ldc.i8":
                    evaluationStack.Push(new StackValue($"{inst.Operand}L"));
                    return null;
                    
                case "ldc.r4":
                    evaluationStack.Push(new StackValue($"{inst.Operand}f"));
                    return null;
                    
                case "ldc.r8":
                    evaluationStack.Push(new StackValue(inst.Operand));
                    return null;
                    
                case "dup":
                    if (evaluationStack.Count > 0)
                    {
                        var top = evaluationStack.Peek();
                        evaluationStack.Push(new StackValue(top.Expression));
                    }
                    return null;
                    
                case "pop":
                    if (evaluationStack.Count > 0)
                    {
                        evaluationStack.Pop();
                    }
                    return null;
                    
                case "call":
                case "callvirt":
                    return ProcessMethodCall(inst);
                    
                case "newobj":
                    return ProcessNewObject(inst);
                    
                case "newarr":
                    if (evaluationStack.Count > 0)
                    {
                        var size = evaluationStack.Pop();
                        var arrayType = GetTypeName(inst.Operand);
                        evaluationStack.Push(new StackValue($"new {arrayType}[{size.Expression}]"));
                    }
                    return null;
                    
                case "ldlen":
                    if (evaluationStack.Count > 0)
                    {
                        var array = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{array.Expression}.Length"));
                    }
                    return null;
                    
                case "ldfld":
                    if (evaluationStack.Count > 0)
                    {
                        var obj = evaluationStack.Pop();
                        var fieldName = GetFieldName(inst.Operand);
                        evaluationStack.Push(new StackValue($"{obj.Expression}.{fieldName}"));
                    }
                    return null;
                    
                case "ldflda":
                    if (evaluationStack.Count > 0)
                    {
                        var obj = evaluationStack.Pop();
                        var fieldName = GetFieldName(inst.Operand);
                        evaluationStack.Push(new StackValue($"{obj.Expression}.{fieldName}", null, true));
                    }
                    return null;
                    
                case "stfld":
                    if (evaluationStack.Count >= 2)
                    {
                        var value = evaluationStack.Pop();
                        var obj = evaluationStack.Pop();
                        var fieldName = GetFieldName(inst.Operand);
                        
                        if (obj.IsFieldAddress)
                        {
                            return $"{obj.Expression}.{fieldName} = {value.Expression};";
                        }
                        else
                        {
                            return $"{obj.Expression}.{fieldName} = {value.Expression};";
                        }
                    }
                    return null;
                    
                case "ldsfld":
                    var staticFieldName = GetFieldName(inst.Operand);
                    var staticFieldType = GetTypeName(inst.Operand);
                    evaluationStack.Push(new StackValue($"{staticFieldType}.{staticFieldName}"));
                    return null;
                    
                case "stsfld":
                    if (evaluationStack.Count > 0)
                    {
                        var value = evaluationStack.Pop();
                        var sfieldName = GetFieldName(inst.Operand);
                        var sfieldType = GetTypeName(inst.Operand);
                        return $"{sfieldType}.{sfieldName} = {value.Expression};";
                    }
                    return null;
                    
                case "ldelem.i1":
                case "ldelem.u1":
                case "ldelem.i2":
                case "ldelem.u2":
                case "ldelem.i4":
                case "ldelem.u4":
                case "ldelem.i8":
                case "ldelem.r4":
                case "ldelem.r8":
                case "ldelem.ref":
                case "ldelem":
                    if (evaluationStack.Count >= 2)
                    {
                        var index = evaluationStack.Pop();
                        var array = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{array.Expression}[{index.Expression}]"));
                    }
                    return null;
                    
                case "stelem.i":
                case "stelem.i1":
                case "stelem.i2":
                case "stelem.i4":
                case "stelem.i8":
                case "stelem.r4":
                case "stelem.r8":
                case "stelem.ref":
                case "stelem":
                    if (evaluationStack.Count >= 3)
                    {
                        var value = evaluationStack.Pop();
                        var index = evaluationStack.Pop();
                        var array = evaluationStack.Pop();
                        return $"{array.Expression}[{index.Expression}] = {value.Expression};";
                    }
                    return null;
                    
                case "box":
                    if (evaluationStack.Count > 0)
                    {
                        var val = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"(object){val.Expression}"));
                    }
                    return null;
                    
                case "unbox":
                case "unbox.any":
                    if (evaluationStack.Count > 0)
                    {
                        var obj = evaluationStack.Pop();
                        var type = GetTypeName(inst.Operand);
                        evaluationStack.Push(new StackValue($"({type}){obj.Expression}"));
                    }
                    return null;
                    
                case "castclass":
                    if (evaluationStack.Count > 0)
                    {
                        var obj = evaluationStack.Pop();
                        var type = GetTypeName(inst.Operand);
                        evaluationStack.Push(new StackValue($"({type}){obj.Expression}"));
                    }
                    return null;
                    
                case "isinst":
                    if (evaluationStack.Count > 0)
                    {
                        var obj = evaluationStack.Pop();
                        var type = GetTypeName(inst.Operand);
                        evaluationStack.Push(new StackValue($"{obj.Expression} as {type}"));
                    }
                    return null;
                    
                case "throw":
                    if (evaluationStack.Count > 0)
                    {
                        var ex = evaluationStack.Pop();
                        return $"throw {ex.Expression};";
                    }
                    return "throw;";
                    
                case "rethrow":
                    return "throw;";
                    
                case "br.s":
                case "br":
                    
                    var targetLabel = inst.Operand?.Replace("IL_", "label_");
                    return $"goto {targetLabel};";
                    
                case "brtrue.s":
                case "brtrue":
                    if (evaluationStack.Count > 0)
                    {
                        var condition = evaluationStack.Pop();
                        return $"if ({condition.Expression})";
                    }
                    return null;
                    
                case "brfalse.s":
                case "brfalse":
                    if (evaluationStack.Count > 0)
                    {
                        var condition = evaluationStack.Pop();
                        return $"if (!{condition.Expression})";
                    }
                    return null;
                    
                case "beq.s":
                case "beq":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        return $"if ({val1.Expression} == {val2.Expression})";
                    }
                    return null;
                    
                case "bne.un.s":
                case "bne.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        return $"if ({val1.Expression} != {val2.Expression})";
                    }
                    return null;
                    
                case "blt.s":
                case "blt":
                case "blt.un.s":
                case "blt.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        var loopVar = val1.Expression;
                        var limit = val2.Expression;
                        
                        
                        if (loopVar.StartsWith("index") || loopVar.StartsWith("num"))
                        {
                            return $"for (int {loopVar} = 0; {loopVar} < {limit}; ++{loopVar})";
                        }
                        return $"if ({val1.Expression} < {val2.Expression})";
                    }
                    return null;
                    
                case "ble.s":
                case "ble":
                case "ble.un.s":
                case "ble.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        return $"if ({val1.Expression} <= {val2.Expression})";
                    }
                    return null;
                    
                case "bgt.s":
                case "bgt":
                case "bgt.un.s":
                case "bgt.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        return $"if ({val1.Expression} > {val2.Expression})";
                    }
                    return null;
                    
                case "bge.s":
                case "bge":
                case "bge.un.s":
                case "bge.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        return $"if ({val1.Expression} >= {val2.Expression})";
                    }
                    return null;
                    
                case "switch":
                    if (evaluationStack.Count > 0)
                    {
                        var switchValue = evaluationStack.Pop();
                        return $"switch ({switchValue.Expression})";
                    }
                    return null;
                    
                case "add":
                case "add.ovf":
                case "add.ovf.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} + {val2.Expression}"));
                    }
                    return null;
                    
                case "sub":
                case "sub.ovf":
                case "sub.ovf.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} - {val2.Expression}"));
                    }
                    return null;
                    
                case "mul":
                case "mul.ovf":
                case "mul.ovf.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} * {val2.Expression}"));
                    }
                    return null;
                    
                case "div":
                case "div.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} / {val2.Expression}"));
                    }
                    return null;
                    
                case "rem":
                case "rem.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} % {val2.Expression}"));
                    }
                    return null;
                    
                case "and":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} & {val2.Expression}"));
                    }
                    return null;
                    
                case "or":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} | {val2.Expression}"));
                    }
                    return null;
                    
                case "xor":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} ^ {val2.Expression}"));
                    }
                    return null;
                    
                case "shl":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} << {val2.Expression}"));
                    }
                    return null;
                    
                case "shr":
                case "shr.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"{val1.Expression} >> {val2.Expression}"));
                    }
                    return null;
                    
                case "neg":
                    if (evaluationStack.Count > 0)
                    {
                        var val = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"-{val.Expression}"));
                    }
                    return null;
                    
                case "not":
                    if (evaluationStack.Count > 0)
                    {
                        var val = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"~{val.Expression}"));
                    }
                    return null;
                    
                case "ceq":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"({val1.Expression} == {val2.Expression} ? 1 : 0)"));
                    }
                    return null;
                    
                case "cgt":
                case "cgt.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"({val1.Expression} > {val2.Expression} ? 1 : 0)"));
                    }
                    return null;
                    
                case "clt":
                case "clt.un":
                    if (evaluationStack.Count >= 2)
                    {
                        var val2 = evaluationStack.Pop();
                        var val1 = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"({val1.Expression} < {val2.Expression} ? 1 : 0)"));
                    }
                    return null;
                    
                case "conv.i":
                case "conv.i1":
                case "conv.i2":
                case "conv.i4":
                case "conv.i8":
                case "conv.u":
                case "conv.u1":
                case "conv.u2":
                case "conv.u4":
                case "conv.u8":
                case "conv.r4":
                case "conv.r8":
                case "conv.r.un":
                    if (evaluationStack.Count > 0)
                    {
                        var val = evaluationStack.Pop();
                        var targetType = GetConversionType(inst.OpCode);
                        evaluationStack.Push(new StackValue($"({targetType}){val.Expression}"));
                    }
                    return null;
                    
                case "ldind.i1":
                case "ldind.u1":
                case "ldind.i2":
                case "ldind.u2":
                case "ldind.i4":
                case "ldind.u4":
                case "ldind.i8":
                case "ldind.i":
                case "ldind.r4":
                case "ldind.r8":
                case "ldind.ref":
                    if (evaluationStack.Count > 0)
                    {
                        var addr = evaluationStack.Pop();
                        evaluationStack.Push(new StackValue($"*{addr.Expression}"));
                    }
                    return null;
                    
                case "stind.i":
                case "stind.i1":
                case "stind.i2":
                case "stind.i4":
                case "stind.i8":
                case "stind.r4":
                case "stind.r8":
                case "stind.ref":
                    if (evaluationStack.Count >= 2)
                    {
                        var value = evaluationStack.Pop();
                        var addr = evaluationStack.Pop();
                        return $"*{addr.Expression} = {value.Expression};";
                    }
                    return null;
                    
                case "leave":
                case "leave.s":
                    
                    return null;
                    
                case "endfinally":
                    
                    return null;
                    
                case "endfilter":
                    
                    return null;
                    
                case "ldtoken":
                    var token = inst.Operand;
                    evaluationStack.Push(new StackValue($"typeof({GetTypeName(token)})"));
                    return null;
                    
                case "sizeof":
                    var sizeType = GetTypeName(inst.Operand);
                    evaluationStack.Push(new StackValue($"sizeof({sizeType})"));
                    return null;
                    
                default:
                    return $"// Unhandled: {inst.OpCode} {inst.Operand ?? ""}";
            }
        }
        
        private string GetConversionType(string opCode)
        {
            switch (opCode)
            {
                case "conv.i1":
                    return "sbyte";
                case "conv.u1":
                    return "byte";
                case "conv.i2":
                    return "short";
                case "conv.u2":
                    return "ushort";
                case "conv.i4":
                    return "int";
                case "conv.u4":
                    return "uint";
                case "conv.i8":
                    return "long";
                case "conv.u8":
                    return "ulong";
                case "conv.r4":
                    return "float";
                case "conv.r8":
                    return "double";
                case "conv.i":
                    return "IntPtr";
                case "conv.u":
                    return "UIntPtr";
                case "conv.r.un":
                    return "double";
                default:
                    return "object";
            }
        }

        private string ProcessMethodCall(ILInstruction inst)
        {
            var methodInfo = inst.Operand;
            var methodName = GetMethodName(methodInfo);
            var isStatic = IsStaticCall(methodInfo);
            
            
            var argCount = GetArgumentCount(methodInfo);
            var args = new List<string>();
            
            for (int i = 0; i < argCount; i++)
            {
                if (evaluationStack.Count > 0)
                {
                    args.Insert(0, evaluationStack.Pop().Expression);
                }
            }
            
            string target = null;
            if (!isStatic && evaluationStack.Count > 0)
            {
                target = evaluationStack.Pop().Expression;
            }
            
            var callExpr = isStatic ? 
                $"{GetTypeName(methodInfo)}.{methodName}({string.Join(", ", args.ToArray())})" :
                $"{target}.{methodName}({string.Join(", ", args.ToArray())})";
            
            
            if (!ReturnsVoid(methodInfo))
            {
                evaluationStack.Push(new StackValue(callExpr));
                return null;
            }
            
            return $"{callExpr};";
        }

        private string ProcessNewObject(ILInstruction inst)
        {
            var ctorInfo = inst.Operand;
            var typeName = GetConstructorTypeName(ctorInfo);
            
            
            var argCount = GetConstructorArgCount(ctorInfo);
            var args = new List<string>();
            
            for (int i = 0; i < argCount; i++)
            {
                if (evaluationStack.Count > 0)
                {
                    args.Insert(0, evaluationStack.Pop().Expression);
                }
            }
            
            var newExpr = $"new {typeName}({string.Join(", ", args.ToArray())})";
            evaluationStack.Push(new StackValue(newExpr));
            
            return null;
        }

        private List<string> ExtractUsingStatements(List<string> statements)
        {
            var usingStatements = new List<string>();
            
            foreach (var stmt in statements)
            {
                if (stmt.Contains("FileStream") && stmt.Contains("= new"))
                {
                    var match = Regex.Match(stmt, @"(\w+)\s*=\s*new\s+FileStream\(([^)]+)\)");
                    if (match.Success)
                    {
                        usingStatements.Add($"FileStream {match.Groups[1].Value} = new FileStream({match.Groups[2].Value})");
                    }
                }
                else if (stmt.Contains("BinaryReader") && stmt.Contains("= new"))
                {
                    var match = Regex.Match(stmt, @"(\w+)\s*=\s*new\s+BinaryReader\(([^)]+)\)");
                    if (match.Success)
                    {
                        usingStatements.Add($"BinaryReader {match.Groups[1].Value} = new BinaryReader((Stream) {match.Groups[2].Value})");
                    }
                }
            }
            
            return usingStatements;
        }

        private bool IsResourceDeclaration(string statement)
        {
            return statement.Contains("FileStream") || statement.Contains("BinaryReader") || 
                   statement.Contains("BinaryWriter");
        }

        private string GetMethodName(string methodInfo)
        {
            var parts = methodInfo.Split('.');
            if (parts.Length >= 2)
            {
                var methodName = parts[parts.Length - 1];
                
                if (methodName.StartsWith("get_"))
                {
                    return methodName.Substring(4);
                }
                return methodName;
            }
            return methodInfo;
        }

        private string GetTypeName(string methodInfo)
        {
            var lastDot = methodInfo.LastIndexOf('.');
            if (lastDot > 0)
            {
                var typeName = methodInfo.Substring(0, lastDot);
                var simpleName = typeName.Split('.').Last();
                return simpleName;
            }
            return "";
        }

        private string GetFieldName(string fieldInfo)
        {
            var parts = fieldInfo.Split('.');
            return parts.Length > 0 ? parts[parts.Length - 1] : fieldInfo;
        }

        private string GetConstructorTypeName(string ctorInfo)
        {
            if (ctorInfo.Contains("..ctor"))
            {
                var typePart = ctorInfo.Replace("..ctor", "");
                var parts = typePart.Split('.');
                return parts.Length > 0 ? parts[parts.Length - 1] : typePart;
            }
            return ctorInfo;
        }

        private bool IsStaticCall(string methodInfo)
        {
            
            var staticTypes = new[] { "File", "String", "UserData", "Path", "Directory", "Math" };
            return staticTypes.Any(t => methodInfo.Contains(t + "."));
        }

        private int GetArgumentCount(string methodInfo)
        {
            
            if (methodInfo.Contains("Concat")) return 2;
            if (methodInfo.Contains("Exists")) return 1;
            if (methodInfo.Contains("CompareTo")) return 1;
            if (methodInfo.Contains("Read")) return 0;
            if (methodInfo.Contains("get_")) return 0;
            return 0;
        }

        private int GetConstructorArgCount(string ctorInfo)
        {
            if (ctorInfo.Contains("FileStream")) return 3;
            if (ctorInfo.Contains("BinaryReader")) return 1;
            if (ctorInfo.Contains("BinaryWriter")) return 1;
            if (ctorInfo.Contains("Version")) return 1;
            return 0;
        }

        private bool ReturnsVoid(string methodInfo)
        {
            
            var voidMethods = new[] { "Dispose", "Write", "WriteLine", "Close" };
            return voidMethods.Any(m => methodInfo.Contains(m));
        }

        private void WriteLine(string text)
        {
            output.AppendLine(new string(' ', indentLevel * 2) + text);
        }
    }
    
    
    public class IL2CSharpConverter
    {
        private ILDecompiler decompiler;

        public IL2CSharpConverter()
        {
            decompiler = new ILDecompiler();
        }

        public string Convert(string ilCode, MethodInfo methodInfo = null)
        {
            methodInfo = methodInfo ?? new MethodInfo();
            
            return decompiler.Decompile(
                ilCode,
                methodInfo.Name,
                methodInfo.ReturnType,
                methodInfo.Parameters,
                methodInfo.LocalVariables
            );
        }
        
        public string Convert(ILInstruction[] ilInstructions, MethodInfo methodInfo = null)
        {
            methodInfo = methodInfo ?? new MethodInfo();
            
            return decompiler.Decompile(
                ilInstructions,
                methodInfo.Name,
                methodInfo.ReturnType,
                methodInfo.Parameters,
                methodInfo.LocalVariables
            );
        }
        
        public string Convert(List<ILInstruction> ilInstructions, MethodInfo methodInfo = null)
        {
            methodInfo = methodInfo ?? new MethodInfo();
            
            return decompiler.Decompile(
                ilInstructions,
                methodInfo.Name,
                methodInfo.ReturnType,
                methodInfo.Parameters,
                methodInfo.LocalVariables
            );
        }
        
        
        public static List<ILInstruction> ParseIL(string ilCode)
        {
            var instructions = new List<ILInstruction>();
            var lines = ilCode.Split('\n');
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                
                var match = Regex.Match(trimmed, @"^(IL_[0-9A-Fa-f]+):\s+(\S+)(?:\s+(.+))?$");
                if (match.Success)
                {
                    var label = match.Groups[1].Value;
                    var opCode = match.Groups[2].Value;
                    var operand = match.Groups[3].Success ? match.Groups[3].Value : null;
                    
                    instructions.Add(new ILInstruction(label, opCode, operand));
                }
            }
            
            return instructions;
        }
        
        
        public static ILInstruction CreateInstruction(string label, string opCode, string operand = null)
        {
            return new ILInstruction(label, opCode, operand);
        }
        
        
        public static ILInstruction CreateInstruction(int offset, string opCode, string operand = null)
        {
            return new ILInstruction($"IL_{offset:X4}", opCode, operand);
        }

        public class MethodInfo
        {
            public string Name { get; set; } = "Undefined";
            public string ReturnType { get; set; } = "void";
            public List<string> Parameters { get; set; } = new List<string>();
            public List<string> LocalVariables { get; set; } = new List<string>();
        }
    }
}
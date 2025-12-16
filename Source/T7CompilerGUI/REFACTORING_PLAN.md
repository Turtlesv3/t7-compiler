# Smart Refactoring Plan for T7CompilerGUI

## Overview
MainForm.cs is 8,627 lines with 172+ methods. This plan focuses on extracting responsibilities into service classes and breaking down large methods into smaller, focused ones.

## Critical Issues Identified

### 1. Massive Methods
- **btnCompile_Click**: ~850 lines (2923-3777) - Does validation, parsing, compilation, file I/O, error handling
- **LoadGscConfSettings**: ~95 lines with large switch statement
- **btnInject_Click**: ~150 lines - Injection workflow
- **InjectScript**: Large method with complex logic

### 2. Mixed Responsibilities
- UI logic mixed with business logic
- File I/O mixed with compilation logic
- Error parsing mixed with error display
- Configuration parsing duplicated

### 3. Code Duplication
- gsc.conf parsing appears in both `btnCompile_Click` and `LoadGscConfSettings`
- Error parsing logic is complex and could be reused
- File path handling scattered throughout

## Refactoring Strategy

### Phase 1: Extract Configuration Parser
**Goal**: Centralize gsc.conf parsing logic

**Create**: `Services/GscConfParser.cs`
- Parse gsc.conf file
- Return structured configuration object
- Handle all configuration keys (symbols, scriptlocation, file, noruntime, hot, game, script)
- Remove duplication between `btnCompile_Click` and `LoadGscConfSettings`

**Benefits**: 
- Single source of truth for configuration
- Easier to test
- Reusable across compilation and settings loading

### Phase 2: Extract Compilation Service
**Goal**: Separate compilation workflow from UI

**Create**: `Services/CompilationService.cs`
- Handle compilation workflow
- File collection and processing
- Source token generation
- Conditional compilation
- Compiler invocation
- Result handling

**Extract from btnCompile_Click**:
- `ValidateCompilationInput()` - Input validation
- `CollectGscFiles()` - File collection
- `ProcessSourceFiles()` - Source token processing
- `ProcessConditionals()` - Conditional compilation
- `InvokeCompiler()` - Compiler call
- `HandleCompilationResult()` - Result processing
- `SaveCompilationOutput()` - File writing

**Benefits**:
- Testable compilation logic
- Reusable from other entry points
- Clearer separation of concerns

### Phase 3: Extract Error Handling
**Goal**: Centralize error parsing and formatting

**Create**: `Services/CompilationErrorParser.cs`
- Parse compiler error messages
- Map errors to source files/lines
- Format error messages for display
- Extract context around errors

**Extract from btnCompile_Click**:
- `ParseCompilerError()` - Error message parsing
- `MapErrorToSourceFile()` - File/line mapping
- `GetErrorContext()` - Context extraction

**Benefits**:
- Consistent error handling
- Reusable error parsing
- Easier to improve error messages

### Phase 4: Extract Injection Service
**Goal**: Separate injection workflow from UI

**Create**: `Services/InjectionService.cs`
- Validate injection input
- Read and validate compiled file
- Handle injection process
- Manage injection state

**Extract from btnInject_Click**:
- `ValidateInjectionInput()` - Input validation
- `ReadCompiledFile()` - File reading/validation
- `PerformInjection()` - Injection execution
- `HandleInjectionResult()` - Result processing

**Benefits**:
- Testable injection logic
- Clearer error handling
- Reusable injection workflow

### Phase 5: Break Down Large Methods
**Goal**: Reduce method complexity

**In MainForm.cs**:
- Break `btnCompile_Click` into smaller methods calling CompilationService
- Break `LoadGscConfSettings` to use GscConfParser
- Break `btnInject_Click` into smaller methods calling InjectionService
- Extract file I/O operations to helper methods

**Benefits**:
- Easier to understand
- Easier to test
- Easier to maintain

### Phase 6: Extract Result Classes
**Goal**: Better data flow and type safety

**Create**: `Models/CompilationResult.cs`, `Models/InjectionResult.cs`
- Structured result objects
- Success/failure states
- Error information
- Output paths

**Benefits**:
- Type-safe results
- Clearer method signatures
- Better error handling

## Implementation Order

1. **Phase 1** (GscConfParser) - Foundation for other refactorings
2. **Phase 2** (CompilationService) - Largest impact, reduces btnCompile_Click significantly
3. **Phase 3** (ErrorParser) - Supports CompilationService
4. **Phase 4** (InjectionService) - Similar pattern to compilation
5. **Phase 5** (Method Breakdown) - Clean up remaining large methods
6. **Phase 6** (Result Classes) - Improve type safety

## Files to Create

- `Services/GscConfParser.cs` (NEW)
- `Services/CompilationService.cs` (NEW)
- `Services/CompilationErrorParser.cs` (NEW)
- `Services/InjectionService.cs` (NEW)
- `Models/CompilationResult.cs` (NEW)
- `Models/InjectionResult.cs` (NEW)
- `Models/GscConfSettings.cs` (NEW)

## Files to Modify

- `Forms/MainForm.cs` - Refactor large methods to use services
- `T7CompilerGUI.csproj` - Add new files

## Expected Benefits

- **MainForm.cs**: Reduce from 8,627 lines to ~6,000 lines (30% reduction)
- **Testability**: Business logic separated from UI, easier to unit test
- **Maintainability**: Clear separation of concerns, easier to modify
- **Reusability**: Services can be used from multiple entry points
- **Readability**: Smaller, focused methods are easier to understand

## Risk Mitigation

- Keep public method signatures the same where possible
- Test after each phase
- Use dependency injection patterns for services
- Maintain backward compatibility with existing code


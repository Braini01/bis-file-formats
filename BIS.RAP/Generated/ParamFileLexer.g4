lexer grammar ParamFileLexer;

@header {namespace BIS.RAP.Generated;}

SINGLE_LINE_COMMENT: '//' ~[\r\n]*           -> channel(HIDDEN);
EMPTY_DELIMITED_COMMENT: ('/*/' | '/**/')    -> channel(HIDDEN);
DELIMITED_COMMENT: '/*' .*? '*/'             -> channel(HIDDEN);
PREPROCESSOR_DIRECTIVE: '#' ~[\r\n]*         -> channel(HIDDEN)/*-> mode(PREPROC_MODE)*/;
WHITESPACES: [\r\n \t]                       -> channel(HIDDEN);

Class:              'class';
Delete:             'delete';
Add_Assign:         '+=';
Assign:             '=';
LSBracket:          '[';
RSBracket:          ']';
LCBracket:          '{';
RCBracket:          '}';
Semicolon:          ';';
Colon:              ':';
Comma:              ',';
DoubleQuote:        '"';
Identifier: [a-zA-Z_] [a-zA-Z_0-9]*;

LiteralString: '"' (EnforceEscapeSequence | .)*? '"';
LiteralInteger: Number;
LiteralFloat: DecimalNumber | ScientificNumber;

fragment EnforceEscapeSequence: '\\\\' | '\\"' | '\\\'';
fragment Number: ('-')? [0-9]+;
fragment DecimalNumber:  Number '.' [0-9]+;
fragment ScientificNumber: DecimalNumber ('e'|'E') ('+'|'-') DecimalNumber;
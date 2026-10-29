#!/usr/bin/env python3
"""Verificacao de organizacao somente leitura; Python 3, biblioteca padrao."""
from pathlib import Path
from collections import defaultdict
from urllib.parse import unquote
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
OWNED = ROOT / 'Assets/MemoriasTecidas'
CODE = OWNED / 'Codigo'


def main():
    errors = []
    scripts = sorted(CODE.rglob('*.cs'))
    if not scripts:
        errors.append('Codigo do jogo ausente em Assets/MemoriasTecidas/Codigo.')
    for f in sorted(OWNED.rglob('*.cs')):
        if CODE not in f.parents:
            errors.append('Script fora de Codigo: ' + f.relative_to(ROOT).as_posix())
    resources = defaultdict(list)
    for f in (ROOT / 'Assets').rglob('*'):
        if not f.is_file() or f.suffix == '.meta' or 'Resources' not in f.parts:
            continue
        parts = f.parts[f.parts.index('Resources') + 1:]
        key = Path(*parts).with_suffix('').as_posix()
        resources[key].append(f)
    for key, files in resources.items():
        if len(files) > 1:
            errors.append('Chave Resources duplicada: ' + key)
    for f in scripts:
        rel = f.relative_to(ROOT).as_posix()
        try:
            text = f.read_text(encoding='utf-8-sig')
        except UnicodeDecodeError:
            errors.append('Codigo nao UTF-8: ' + rel)
            continue
        if '\t' in text or any(line.rstrip() != line for line in text.splitlines()):
            errors.append('Tabulacao ou espaco final no codigo: ' + rel)
        if not text.endswith('\n'):
            errors.append('Falta quebra final: ' + rel)
        if 'Editor' not in f.parts and re.search(r'^\s*using\s+UnityEditor\b', text, re.M):
            errors.append('Dependencia de Editor no Runtime: ' + rel)
        if 'Assets/MemoriasTecidas/MundoV2/' in text:
            errors.append('Caminho fisico antigo no codigo: ' + rel)
        for component in re.findall(r'\bclass\s+(\w+)\s*:\s*(?:UnityEngine\.)?(?:MonoBehaviour|ScriptableObject)\b', text):
            if f.stem != component:
                errors.append('Nome de arquivo/componente divergente: ' + rel + ' / ' + component)
        for key in re.findall(r'Resources\.Load(?:<[^>]+>)?\(\s*"([^"\n]+)"\s*\)', text):
            if key not in resources:
                errors.append('Resources literal ausente: ' + key + ' em ' + rel)
    for f in OWNED.rglob('*'):
        if f.suffix not in ('.shader', '.cginc', '.hlsl'):
            continue
        for target in re.findall(r'#include\s+"(Assets/[^"\n]+)"', f.read_text(encoding='utf-8-sig')):
            if not (ROOT / target).is_file():
                errors.append('Include ausente: ' + target)
    docs = [ROOT / 'README.md', ROOT / 'CONTRIBUTING.md', *sorted((ROOT / 'Docs').glob('*.md'))]
    for f in docs:
        if not f.is_file():
            errors.append('Guia obrigatorio ausente: ' + f.name)
            continue
        for target in re.findall(r'(?<!!)\[[^\]]+\]\(([^)]+)\)', f.read_text(encoding='utf-8')):
            if re.match(r'^(?:https?://|mailto:|#)', target):
                continue
            path = unquote(target.split('#', 1)[0])
            if path and not (f.parent / path).exists():
                errors.append('Link local ausente em ' + f.name + ': ' + path)
    lower, upper = ROOT / 'docs', ROOT / 'Docs'
    if lower.is_dir() and upper.is_dir() and not lower.samefile(upper):
        errors.append('Existem Docs e docs: unifique a documentacao atual.')
    for error in errors:
        print('ERRO:', error)
    print('Organizacao:', len(scripts), 'scripts ativos;', len(resources), 'chaves Resources;', len(errors), 'erro(s).')
    print('Escopo: estrutura e convencoes. Compilacao, Play e build exigem a Unity.')
    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main())

import os
import re

def remove_comments_and_tooltips(text):
    # Regex for [Tooltip("...")] that handle alone Tooltips
    text = re.sub(r'^\s*\[\s*Tooltip\s*\([^)]*\)\s*\]\s*\n', '', text, flags=re.MULTILINE)
    text = re.sub(r'\[\s*Tooltip\s*\([^)]*\)\s*\]', '', text)
    
    # Handle Tooltip inside mixed attributes like [SerializeField, Tooltip("...")] 
    # e.g., , Tooltip("...") or Tooltip("..."), 
    text = re.sub(r',\s*Tooltip\s*\([^)]*\)', '', text)
    text = re.sub(r'Tooltip\s*\([^)]*\)\s*,', '', text)
    
    # Regex for C# line comments and block comments
    # To avoid breaking strings like "http://", we match strings too and return them unchanged.
    pattern = re.compile(
        r'//.*?$|/\*.*?\*/|"(?:\\.|[^"\\])*"',
        re.DOTALL | re.MULTILINE
    )
    
    def replacer(match):
        s = match.group(0)
        if s.startswith('/'):
            return ''
        else:
            return s

    text = pattern.sub(replacer, text)
    
    # Remove multiple spaces/tabs before a newline (trailing spaces left by removed line comments)
    text = re.sub(r'[ \t]+$', '', text, flags=re.MULTILINE)
    
    # Clean up multiple empty lines
    text = re.sub(r'\n{3,}', '\n\n', text)
    
    return text

def process_directory(directory):
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith('.cs'):
                filepath = os.path.join(root, file)
                try:
                    with open(filepath, 'r', encoding='utf-8') as f:
                        content = f.read()
                    
                    new_content = remove_comments_and_tooltips(content)
                    
                    if new_content != content:
                        with open(filepath, 'w', encoding='utf-8') as f:
                            f.write(new_content)
                        print(f"Cleaned: {filepath}")
                except Exception as e:
                    print(f"Error processing {filepath}: {e}")

if __name__ == '__main__':
    script_dir = r"c:\Users\user\Desktop\S\UnityRepo\Unity-Personal-Project\Assets\3.Script"
    process_directory(script_dir)

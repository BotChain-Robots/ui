#!/usr/bin/env python3
"""Bulk-convert TextMeshProUGUI components to Unity UI Text in a .unity scene file."""
import re, sys

TMP_GUID = "f4688fdb7df04437aeb418b961361dc5"
TEXT_GUID = "5f7201a12d95ffc409449d95f23cf332"
ARIAL_FONT = "{fileID: 10102, guid: 0000000000000000e000000000000000, type: 0}"

# Already converted IDs (skip these)
ALREADY_DONE = {"960000007", "1501844973", "1385057283"}

def extract_prop(block, key):
    m = re.search(rf'^\s+{key}:\s*(.+)$', block, re.MULTILINE)
    return m.group(1).strip() if m else None

def alignment_from_tmp(h_align, v_align):
    h = int(h_align) if h_align else 1
    v = int(v_align) if v_align else 256
    col = {1: 0, 2: 1, 4: 2, 8: 0}.get(h, 0)  # Left=0, Center=1, Right=2
    row = {256: 0, 512: 1, 1024: 2}.get(v, 0)   # Upper=0, Middle=1, Lower=2
    return row * 3 + col

def convert_block(block, file_id, game_obj_id):
    text = extract_prop(block, "m_text") or ""
    font_size = extract_prop(block, "m_fontSize") or "22"
    font_style = extract_prop(block, "m_fontStyle") or "0"
    color_r = color_g = color_b = color_a = None
    color_match = re.search(r'm_Color:\s*\{r:\s*([\d.]+),\s*g:\s*([\d.]+),\s*b:\s*([\d.]+),\s*a:\s*([\d.]+)\}', block)
    if color_match:
        color_r, color_g, color_b, color_a = color_match.groups()
    raycast = extract_prop(block, "m_RaycastTarget") or "1"
    h_align = extract_prop(block, "m_HorizontalAlignment")
    v_align = extract_prop(block, "m_VerticalAlignment")
    alignment = alignment_from_tmp(h_align, v_align)
    
    color_str = f"{{r: {color_r}, g: {color_g}, b: {color_b}, a: {color_a}}}" if color_r else "{r: 1, g: 1, b: 1, a: 1}"
    
    font_size_int = int(float(font_size))
    
    return f"""--- !u!114 &{file_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {game_obj_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {TEXT_GUID}, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_Material: {{fileID: 0}}
  m_Color: {color_str}
  m_RaycastTarget: {raycast}
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_FontData:
    m_Font: {ARIAL_FONT}
    m_FontSize: {font_size_int}
    m_FontStyle: {font_style}
    m_BestFit: 0
    m_MinSize: 10
    m_MaxSize: 40
    m_Alignment: {alignment}
    m_AlignByGeometry: 0
    m_RichText: 1
    m_HorizontalOverflow: 0
    m_VerticalOverflow: 0
    m_LineSpacing: 1
  m_Text: {text}"""

def main():
    path = sys.argv[1] if len(sys.argv) > 1 else "Assets/Scenes/SampleScene.unity"
    with open(path, 'r') as f:
        content = f.read()
    
    # Split into YAML documents (each starting with --- !u!)
    parts = re.split(r'(--- !u!\d+ &\d+)', content)
    
    converted = 0
    result = []
    i = 0
    while i < len(parts):
        if i + 1 < len(parts) and parts[i].startswith('--- !u!'):
            header = parts[i]
            body = parts[i + 1]
            
            file_id_match = re.search(r'&(\d+)', header)
            file_id = file_id_match.group(1) if file_id_match else None
            
            if file_id and file_id not in ALREADY_DONE and TMP_GUID in body:
                game_obj_match = re.search(r'm_GameObject:\s*\{fileID:\s*(\d+)', body)
                game_obj_id = game_obj_match.group(1) if game_obj_match else "0"
                
                full_block = header + body
                new_block = convert_block(full_block, file_id, game_obj_id)
                
                # Find where the next --- !u! starts in body, keep the rest
                next_header_pos = body.find('\n--- !u!')
                if next_header_pos >= 0:
                    remainder = body[next_header_pos:]
                    result.append(new_block + remainder)
                else:
                    result.append(new_block + '\n')
                
                converted += 1
                i += 2
                continue
            
            result.append(header)
            result.append(body)
            i += 2
        else:
            result.append(parts[i])
            i += 1
    
    output = ''.join(result)
    with open(path, 'w') as f:
        f.write(output)
    
    print(f"Converted {converted} TMP components to Unity Text")

if __name__ == '__main__':
    main()

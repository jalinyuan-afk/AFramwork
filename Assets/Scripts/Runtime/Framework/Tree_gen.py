#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
自动生成 Core 框架类文件树状图文档
生成带类名和功能说明的 Markdown 树状结构
"""

import os
import re

def extract_class_info(filepath):
    """提取 C# 文件的类名和注释摘要"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            lines = content.split('\n')
            
        class_name = ""
        summary = ""
        
        # 提取 XML 注释中的 summary
        summary_match = re.search(r'/// <summary>\s*\n\s*/// (.*?)\s*\n\s*/// </summary>', content, re.DOTALL)
        if summary_match:
            summary = summary_match.group(1).strip()
            summary = re.sub(r'///\s*', '', summary)
            summary = summary.replace('\n', ' ')[:100]  # 限制长度
        
        # 提取类名/接口名/枚举名
        for line in lines:
            # 匹配 public class/interface/enum/struct
            class_match = re.search(r'public\s+(class|interface|enum|struct)\s+(\w+)', line)
            if class_match:
                class_name = class_match.group(2)
                break
            # 匹配非 public 的定义
            if not class_name:
                class_match = re.search(r'(class|interface|enum|struct)\s+(\w+)', line)
                if class_match:
                    class_name = class_match.group(2)
                    break
        
        return class_name, summary
    except Exception as e:
        return "", f"(读取失败: {str(e)})"

def scan_directory(root_path, prefix="", output_lines=None, is_last=True):
    """递归扫描目录并生成树状图"""
    if output_lines is None:
        output_lines = []
    
    try:
        items = sorted(os.listdir(root_path))
        # 过滤掉 .meta 和 .git 文件
        items = [item for item in items if not item.endswith('.meta') and item != '.git']
        
        dirs = [item for item in items if os.path.isdir(os.path.join(root_path, item))]
        files = [item for item in items if os.path.isfile(os.path.join(root_path, item)) and item.endswith('.cs')]
        
        # 先处理文件
        for i, filename in enumerate(files):
            is_last_item = (i == len(files) - 1) and len(dirs) == 0
            connector = "└── " if is_last_item else "├── "
            
            filepath = os.path.join(root_path, filename)
            class_name, summary = extract_class_info(filepath)
            
            if class_name and summary:
                line = f"{prefix}{connector}{filename} ← {class_name} ({summary})"
            elif class_name:
                line = f"{prefix}{connector}{filename} ← {class_name}"
            else:
                line = f"{prefix}{connector}{filename}"
            
            output_lines.append(line)
        
        # 再处理子目录
        for i, dirname in enumerate(dirs):
            is_last_dir = (i == len(dirs) - 1)
            connector = "└── " if is_last_dir else "├── "
            
            output_lines.append(f"{prefix}{connector}{dirname}/")
            
            # 递归扫描子目录
            new_prefix = prefix + ("    " if is_last_dir else "│   ")
            subdir_path = os.path.join(root_path, dirname)
            scan_directory(subdir_path, new_prefix, output_lines, is_last_dir)
        
        return output_lines
    
    except Exception as e:
        output_lines.append(f"{prefix}[错误: {str(e)}]")
        return output_lines

def generate_markdown(root_path, output_file):
    """生成完整的 Markdown 文档"""
    print(f"开始扫描目录: {root_path}")
    
    output_lines = [
        "# Framework框架类文件树状结构",
        "",
        "> 自动生成的类文件位置与功能说明",
        "",
        "```",
        "Assets/TradeGame/Scripts/Runtime/Framework/"
    ]
    
    # 扫描目录
    tree_lines = scan_directory(root_path, "", [], True)
    output_lines.extend(tree_lines)
    
    output_lines.append("```")
    output_lines.append("")
    output_lines.append(f"**总计**: {len([l for l in tree_lines if '.cs' in l])} 个 C# 文件")
    
    # 写入文件
    content = '\n'.join(output_lines)
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write(content)
    
    print(f"✅ 树状图已生成: {output_file}")
    print(f"   共扫描 {len([l for l in tree_lines if '.cs' in l])} 个 C# 文件")

if __name__ == "__main__":
    # 当前脚本所在目录就是 Core 目录
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_path = os.path.join(script_dir, "tree_output.md")
    
    generate_markdown(script_dir, output_path)
    
    print("\n使用方法:")
    print("  1. 在 Core 目录运行: python tree_gen.py")
    print("  2. 查看生成的 tree_output.md 文件")
    print("  3. 复制内容到 README_CORE_MODULE.md")
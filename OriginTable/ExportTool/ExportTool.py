import os
import pandas as pd
import tkinter as tk
from tkinter import ttk
import subprocess

def get_git_root_path():
    try:
        root = subprocess.check_output(['git', 'rev-parse', '--show-toplevel']).strip().decode('utf-8')
    except subprocess.CalledProcessError:
        print("Error: Not a git repository")
        return None
    return root

def export_excel_to_csv():
    try:
        git_root = get_git_root_path()

        if git_root:
            excel_dir = os.path.normpath(os.path.join(git_root, 'OriginTable'))
            csv_dir = os.path.normpath(os.path.join(git_root, 'BOOOM2024', 'Assets', 'Resources', 'Table'))
            
            print(excel_dir)
            print(csv_dir)
            
            # 确保CSV目录存在
            if not os.path.exists(csv_dir):
                os.makedirs(csv_dir)

            # 遍历Excel目录，找到所有Excel文件
            for filename in os.listdir(excel_dir):
                if filename.endswith('.xlsx') or filename.endswith('.xls'):
                    # 读取Excel文件
                    excel_path = os.path.join(excel_dir, filename)
                    xls = pd.ExcelFile(excel_path)
                    
                    # 遍历Excel中的所有sheet
                    for sheet_name in xls.sheet_names:
                        # 读取前三行
                        header_df = pd.read_excel(excel_path, sheet_name=sheet_name, nrows=3)
                        print("Header DF shape:", header_df.shape)
                        print("Header DF sample:", header_df.head())

                        # 读取剩下的数据，跳过前三行，并设置列名
                        data_df = pd.read_excel(excel_path, sheet_name=sheet_name, skiprows=3, names=header_df.columns)
                        print("Data DF shape:", data_df.shape)
                        print("Data DF sample:", data_df.head())

                        # 如果存在'IsExport'列，并且该列有非NaN的值，过滤掉标记为“不导出”的行
                        if 'IsExport' in data_df.columns and data_df['IsExport'].notna().any():
                            data_df = data_df[data_df['IsExport'] != 1]
                        print("Filtered Data DF shape:", data_df.shape)
                        print("Filtered Data DF sample:", data_df.head())

                        # 重置data_df的索引
                        data_df.reset_index(drop=True, inplace=True)

                        # 确保两个数据框有相同的列
                        data_df = data_df.reindex(columns=header_df.columns)

                        # 合并两个数据框
                        final_df = pd.concat([header_df, data_df], ignore_index=True)
                        print("Final DF shape:", final_df.shape)
                        print("Final DF sample:", final_df.head())

                        # 保存为CSV文件
                        csv_filename = f"{sheet_name}.csv"
                        csv_path = os.path.join(csv_dir, csv_filename)
                        final_df.to_csv(csv_path, index=False)

            success_label.config(text="导表成功")
        else:
            success_label.config(text="导表失败，不是一个Git仓库")
        
    except Exception as e:
        print(e)  # 输出错误信息到控制台，方便调试
        success_label.config(text="导表失败")

# 创建Tkinter窗口
root = tk.Tk()
root.title("Excel to CSV Export Tool")
root.geometry("300x200")  # 设置窗口大小

# 添加按钮
export_button = ttk.Button(root, text="导表", command=export_excel_to_csv)
export_button.pack(pady=20)

# 添加成功标签
success_label = ttk.Label(root, text="")
success_label.pack(pady=10)

# 运行Tkinter事件循环
root.mainloop()
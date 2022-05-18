using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IOTools 
{
    /// <summary>
    /// 删除所有文件
    /// </summary>
    /// <param name="dirPath"></param>
    public static void DeleteAllFiles(string dirPath)
    {
        if (Directory.Exists(dirPath))
        {
            //获取该路径下的文件路径
            string[] filePathList = Directory.GetFiles(dirPath);
            foreach (string filePath in filePathList)
            {
                File.Delete(filePath);
            }
        }
    }

    /// <summary>
    /// 删除目录下的所有目录
    /// </summary>
    /// <param name="dirPath"></param>
    public static void DeleteAllDirectory(string dirPath)
    {
        if (Directory.Exists(dirPath))
        {
            DeleteAllFiles(dirPath);
            //获取该路径下的文件夹路径
            string[] directorsList = Directory.GetDirectories(dirPath);
            foreach (string directory in directorsList)
            {
                Directory.Delete(directory, true);//删除该文件夹及该文件夹下包含的文件
            }

            Directory.Delete(dirPath, true);
        }
    }
}

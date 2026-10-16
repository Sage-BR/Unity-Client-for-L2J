using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecipeTable
{
    private static RecipeTable _instance;
    private static Dictionary<int, RecipeData> _recipesData;

    private int _indexName = 0;
    private int _indexid_mk = 1;
    private int _indexRecipeId = 2;
    private int _indexlevel = 3;
    private int _indexItemId = 4;
    private int _indexCount = 5;
    private int _indexMpCost = 6;
    private int _indexSuccesRate = 7;
    public static RecipeTable Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new RecipeTable();
                _recipesData = new Dictionary<int, RecipeData>();
            }

            return _instance;
        }
    }


    public RecipeData GetRecipeData(int recipeId)
    {
        if (_recipesData.ContainsKey(recipeId))
        {
            return _recipesData[recipeId];
        }
        return null;
    }






    public void Initialize()
    {
        try
        {
            string dataPathE = Path.Combine(Application.streamingAssetsPath, "Data/Meta/Recipe-c_interlude.txt");
            ReadRecipeInterlude(dataPathE);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize RecipeTable: {ex.Message}");
        }
    }

    public void ReadRecipeInterlude(string dataPath)
    {

        using (StreamReader reader = new StreamReader(dataPath))
        {
            string line;
            int index = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (index != 0)
                {
                    string[] ids = line.Split('\t');
                    int id = Int32.Parse(ids[_indexid_mk]);
                    RecipeData recipeData = CreateModel(ids, id);
                    if (!_recipesData.ContainsKey(recipeData.IDMk))
                    {
                        _recipesData.Add(recipeData.IDMk , recipeData);
                    }
                }
                index++;
            }

        }

    }

    private RecipeData CreateModel(string[] ids, int idMk)
    {
        RecipeData recipe = new RecipeData();

        string aliase = DatUtils.CleanupStringOldData(ids[_indexName]);
        recipe.Alias = aliase;
        recipe.IDMk = idMk;
        recipe.RecipeId = Int32.Parse(ids[_indexRecipeId]); ;
        recipe.Level = Int32.Parse(ids[_indexlevel]);
        recipe.ItemId = Int32.Parse(ids[_indexItemId]);
        recipe.Count = Int32.Parse(ids[_indexCount]);
        recipe.MpCost = Int32.Parse(ids[_indexMpCost]);
        recipe.SuccessRate = Int32.Parse(ids[_indexSuccesRate]);

        int materialCount = Int32.Parse(ids[8]);
        if (materialCount < 0 || materialCount > 10)
            throw new FormatException($"Invalid materials_cnt {materialCount} in recipe {idMk}.");
        recipe.Materials = new RecipeMaterials[10];
        for (int i = 0; i < materialCount; i++)
            SetMaterialSafely(i, recipe, ids);
        return recipe;
    }

    void SetMaterialSafely(int materialIndex, RecipeData recipe, string[] ids)
    {
        int idIndex = 9 + materialIndex * 2;
        int materialId = RequiredInt(ids, idIndex);
        int count = RequiredInt(ids, idIndex + 1);
        if (materialId <= 0 || count <= 0)
            throw new FormatException($"Invalid ingredient {materialIndex} in recipe {recipe.IDMk}.");
        recipe.SetMaterials(materialId, count, materialIndex);
    }

    private static int RequiredInt(string[] fields, int index)
    {
        if (index >= fields.Length || !int.TryParse(fields[index], out int result))
            throw new FormatException($"Missing or invalid required integer at recipe column {index}.");
        return result;
    }
}
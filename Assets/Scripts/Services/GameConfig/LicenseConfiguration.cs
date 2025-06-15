using UnityEngine;
using System.Collections.Generic;
using Core.Models;

[CreateAssetMenu(fileName = "LicenseConfiguration", menuName = "Supermarket/License System/License Configuration")]
public class LicenseConfiguration : ScriptableObject
{
    [Header("Available Licenses")]
    public List<ProductLicense> AllLicenses = new List<ProductLicense>();
} 
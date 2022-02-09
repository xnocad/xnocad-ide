﻿using IDE.Core.Common;
using IDE.Core.Common.Utilities;
using IDE.Core.Documents;
using IDE.Core.Errors;
using IDE.Core.Interfaces;
using IDE.Core.Storage;
using IDE.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IDE.Core.Compilation
{
    public class Compiler
    {
        public Compiler()
        {
            output = ServiceProvider.GetToolWindow<IOutput>();
            application = ServiceProvider.Resolve<IApplicationViewModel>();//ServiceProvider.GetService<IApplicationViewModel>();
            errors = ServiceProvider.GetToolWindow<IErrorsToolWindowViewModel>();

            //output.Clear();
            //errors.Clear();
        }

        IOutput output;
        IErrorsToolWindowViewModel errors;

        List<ISolutionProjectNodeModel> solutionProjects = new List<ISolutionProjectNodeModel>();

        IApplicationViewModel application;

        /// <summary>
        /// intended to be used when running from a cmd console
        /// </summary>
        /// <param name="solutionFilePath"></param>
        public async Task BuildSolution(string solutionFilePath)
        {
            outputFiles.Clear();

            //load
            LoadSolution(solutionFilePath);

            //create build order
            var orderedProjects = CreateBuildOrder();

            //foreach project: project.Build
            foreach (var project in orderedProjects)
            {
                await BuildProject(project);
            }

            CreateSolutionOutput(Path.GetDirectoryName(solutionFilePath));
        }

        public async Task BuildSolution(ISolutionRootNodeModel solution)
        {
            outputFiles.Clear();

            LoadSolutionProjects(solution);

            //create build order
            var orderedProjects = CreateBuildOrder();

            //foreach project: project.Build
            foreach (var project in orderedProjects)
            {
                await BuildProject(project);
            }

            CreateSolutionOutput(solution.GetItemFolderFullPath());
        }

        List<string> outputFiles = new List<string>();
        void CreateSolutionOutput(string solutionFolder)
        {
            //save output
            var savePath = Path.Combine(solutionFolder, "!Output");//folder
            Directory.CreateDirectory(savePath);

            foreach (var fileOutput in outputFiles)
            {
                var fileName = Path.GetFileName(fileOutput);
                File.Copy(fileOutput, Path.Combine(savePath, fileName), true);
            }
        }

        void LoadSolution(string solutionFilePath)
        {

            //load solution
            var solutionDoc = XmlHelper.Load<SolutionDocument>(solutionFilePath);
            //var solution = new SolutionRootNodeModel { Document = solutionDoc };
            var solution = TypeActivator.CreateInstanceByTypeName<ISolutionRootNodeModel>("SolutionRootNodeModel");

            if (solution == null)
                throw new Exception("Could not create instance of 'SolutionRootNodeModel'");

            solution.Document = solutionDoc;

            LoadSolutionProjects(solution);
        }

        void LoadSolutionProjects(ISolutionRootNodeModel solution)
        {
            solutionProjects.Clear();

            //load projects linear
            var projects = new List<SolutionProjectItem>();
            LoadProjectListLinear(solution.Solution.Children, projects);
            foreach (var p in projects)
            {
                var projectModel = (ISolutionProjectNodeModel)p.CreateSolutionExplorerNodeModel();
                //var projectModel = CreateSolutionExplorerNodeModel(p);
                if (projectModel == null)
                    throw new Exception("var projectModel = CreateSolutionExplorerNodeModel(p);");
                projectModel.ParentNode = solution;
                solutionProjects.Add(projectModel);
            }
        }

        //ISolutionProjectNodeModel CreateSolutionExplorerNodeModel(SolutionProjectItem project)
        //{
        //    var mapper = ServiceProvider.GetService<ISettingsDataToModelMapper>();
        //    if (mapper == null)
        //    {
        //        output.AppendLine("Mapper service 'ISolutionExplorerNodeMapper' was not found");
        //        return null;
        //    }

        //    var projectModel = mapper.CreateSolutionExplorerNodeModel(project);
        //    return projectModel as ISolutionProjectNodeModel;
        //}

        void LoadProjectListLinear(IList<ProjectBaseFileRef> children, List<SolutionProjectItem> projects)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is SolutionProjectItem)
                        projects.Add(child as SolutionProjectItem);
                    else if (child is GroupFolderItem)
                        LoadProjectListLinear((child as GroupFolderItem).Children, projects);
                }
            }
        }

        List<ISolutionProjectNodeModel> CreateBuildOrder()
        {
            //first libraries, then gerbers
            var orderedProjects = solutionProjects.OrderBy(p => p.Project.OutputType).ToList();

            return orderedProjects;
        }

        public async Task BuildProject(ISolutionProjectNodeModel project)
        {
            switch (project.Project.OutputType)
            {
                case ProjectOutputType.Library:
                    await BuildLibraryProject(project);
                    break;
                case ProjectOutputType.Board:
                    await BuildGerberProject(project);
                    break;
            }
        }

        async Task BuildLibraryProject(ISolutionProjectNodeModel project)
        {
            output.AppendLine($"Building project library {project.Name}");

            //create a linear list;
            var libraryItemNodes = new List<ISolutionExplorerNodeModel>();
            CreateProjectItemsLinearList(project.Children.ToList(), libraryItemNodes);

            //load library items
            var files = new List<IFileBaseViewModel>();
            foreach (var slnNode in libraryItemNodes)
            {
                var file = await application.OpenDocumentAsync(slnNode, true);
                files.Add(file);
            }

            //maybe do some processing: update references based on preferences
            //...

            //save into a virtual entity
            //...

            var fileDocuments = files.Where(f => f.Document != null).Select(f => f.Document).ToList();

            var symbols = fileDocuments.OfType<Symbol>().ToList();
            var footprints = fileDocuments.OfType<Footprint>().ToList();
            var components = fileDocuments.OfType<ComponentDocument>().ToList();
            var models = fileDocuments.OfType<ModelDocument>().ToList();
            var fonts = fileDocuments.OfType<FontDocument>().ToList();

            //footprints will reference models from other libs (add them to our lib as libName = 'local'
            #region Models in footprints

            foreach (var fp in footprints)
            {
                if (fp.Models != null)
                {
                    foreach (var model in fp.Models)
                    {
                        try
                        {
                            var modelSearch = models.FirstOrDefault(m => m.Id == model.ModelId);

                            if (modelSearch == null)
                            {
                                modelSearch = project.FindObject(TemplateType.Model, model.ModelLibrary, model.ModelId) as ModelDocument;
                                if (modelSearch != null)
                                {
                                    // modelSearch.Library = null;
                                    models.Add(modelSearch);
                                }
                            }
                        }
                        catch //(Exception ex)
                        {
                        }
                    }
                }
            }
            #endregion

            //component references symbols and footprints (and models)
            #region Component references

            foreach (var cmp in components)
            {
                //symbols
                if (cmp.Gates != null)
                {
                    foreach (var gate in cmp.Gates)
                    {
                        try
                        {
                            var symbolSearch = symbols.FirstOrDefault(s => s.Id == gate.symbolId);
                            if (symbolSearch == null)
                            {
                                symbolSearch = project.FindObject(TemplateType.Symbol, gate.LibraryName, gate.symbolId) as Symbol;
                                if (symbolSearch != null)
                                {
                                    //symbolSearch.Library = null;
                                    symbols.Add(symbolSearch);
                                }
                            }
                        }
                        catch
                        {
                        }
                    }
                }

                //footprint references
                if (cmp.Footprint != null)
                {
                    try
                    {
                        var fptSearch = footprints.FirstOrDefault(f => f.Id == cmp.Footprint.footprintId);
                        if (fptSearch == null)
                        {
                            fptSearch = project.FindObject(TemplateType.Footprint, cmp.Footprint.LibraryName, cmp.Footprint.footprintId) as Footprint;
                            if (fptSearch != null)
                            {
                                //fptSearch.Library = null;
                                footprints.Add(fptSearch);
                            }
                        }

                        //footprint models
                        if (fptSearch != null)
                        {
                            if (fptSearch.Models != null)
                            {
                                foreach (var model in fptSearch.Models)
                                {
                                    try
                                    {
                                        var modelSearch = models.FirstOrDefault(m => m.Id == model.ModelId);

                                        if (modelSearch == null)
                                        {
                                            modelSearch = project.FindObject(TemplateType.Model, model.ModelLibrary, model.ModelId) as ModelDocument;
                                            if (modelSearch != null)
                                            {
                                                // modelSearch.Library = null;
                                                models.Add(modelSearch);
                                            }
                                        }
                                    }
                                    catch //(Exception ex)
                                    {
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            #endregion

            var libDoc = new LibraryDocument { Name = project.Name };
            if (project.Project != null && project.Project.Properties != null)
            {
                libDoc.Namespace = project.Project.Properties.BuildOutputNamespace;
                libDoc.Version = project.Project.Properties.Version;
            }

            libDoc.Symbols = symbols;
            libDoc.Footprints = footprints;
            libDoc.Components = components;
            libDoc.Models = models;

            //save output
            var savePath = Path.Combine(project.GetItemFolderFullPath(), "!Output");//folder
            var outputFolder = savePath;
            Directory.CreateDirectory(savePath);
            savePath = Path.Combine(savePath, libDoc.Name + ".library");
            XmlHelper.Save(libDoc, savePath);

            //save each font file in output doc
            foreach (var fontDoc in fonts)
            {
                var fontPath = Path.Combine(outputFolder, fontDoc.Name + ".font");
                XmlHelper.Save(fontDoc, fontPath);

                outputFiles.Add(fontPath);
            }


            output.AppendLine($"Library built to: {savePath}");

            outputFiles.Add(savePath);
        }

        void CreateProjectItemsLinearList(List<ISolutionExplorerNodeModel> children, List<ISolutionExplorerNodeModel> libraryItems)
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is IProjectReferencesNodeModel)
                        continue;

                    if (child is IProjectFolderNodeModel)
                        CreateProjectItemsLinearList((child as IProjectFolderNodeModel).Children.ToList(), libraryItems);
                    else
                        libraryItems.Add(child);
                }
            }
        }

        async Task BuildGerberProject(ISolutionProjectNodeModel project)
        {
            output.AppendLine($"Building project {project.Name}");
            //todo: highlight errors with red and warnings with orange
            //Error: errors message /n
            //Warning: warning message /n

            //create a linear list;
            var libraryItemNodes = new List<ISolutionExplorerNodeModel>();
            CreateProjectItemsLinearList(project.Children.ToList(), libraryItemNodes);

            //load board items
            var brdFiles = new List<IFileBaseViewModel>();
            foreach (ISolutionExplorerNodeModel slnNode in libraryItemNodes.OfType<IProjectBoardNodeModel>())
            {
                var file = await application.OpenDocumentAsync(slnNode, true);
                brdFiles.Add(file);
            }

            //load schematic files
            var schFiles = new List<IFileBaseViewModel>();
            foreach (ISolutionExplorerNodeModel slnNode in libraryItemNodes.OfType<IProjectSchematicNodeModel>())
            {
                var file = await application.OpenDocumentAsync(slnNode, true);
                schFiles.Add(file);
            }

            //maybe do some processing: update references based on preferences
            //...

            //save into a virtual entity
            //...


            foreach (IBoardDesigner board in brdFiles)
            {
                await board.Build();

                outputFiles.AddRange(board.OutputFiles);
            }

            foreach(ISchematicDesigner sch in schFiles)
            {
                await sch.Build();

                outputFiles.AddRange(sch.OutputFiles);
            }

            var outputFolder = Path.Combine(project.GetItemFolderFullPath(), "!Output");//folder
            output.AppendLine($"Project built to: {outputFolder}");
        }

        public async Task CompileSolution(ISolutionRootNodeModel solution)
        {
            LoadSolutionProjects(solution);

            //create build order
            var orderedProjects = CreateBuildOrder();

            //foreach project: project.Build
            foreach (var project in orderedProjects)
            {
                await CompileProject(project);
            }
        }

        public async Task CompileProject(ISolutionProjectNodeModel project)
        {
            output.AppendLine($"Compiling project {project.Name}");

            //create a linear list;
            var solNodes = new List<ISolutionExplorerNodeModel>();
            var files = new List<IFileBaseViewModel>();

            CreateProjectItemsLinearList(project.Children.ToList(), solNodes);


            foreach (var slnNode in solNodes)
            {
                try
                {
                    var file = await application.OpenDocumentAsync(slnNode, true);
                    files.Add(file);
                    await file.Compile();

                }
                catch (Exception ex)
                {
                    output.AppendLine($"Error: {ex.Message}");
                    errors.AddError(ex.Message, slnNode.Name, project.Name);
                }

            }

            //collect errors (we do it in a separate loop so that the prev loop be executed in Parallel)
            foreach (var file in files)
            {
                foreach (var err in file.CompileErrors)
                {
                    output.AppendLine($"Error: {err.Description}");
                    errors.AddErrorMessage(err);
                }
            }

            var hasErrors = files.Any(f => f.CompileErrors.Count > 0);
            var resultMessage = hasErrors ? "with errors" : "successfully";

            output.AppendLine($"Project {project.Name} compiled {resultMessage}");
        }
    }
}
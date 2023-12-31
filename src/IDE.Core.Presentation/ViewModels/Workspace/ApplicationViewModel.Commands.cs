﻿namespace IDE.Core.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Input;
    using Core.Commands;
    using Core.Utilities;
    using IDE.Core.Interfaces;
    using IDE.Core.MRU;

    public partial class ApplicationViewModel
    {

        /// <summary>
        /// Bind a window to some commands to be executed by the viewmodel.
        /// </summary>
        public void InitCommandBinding(ILayoutableWindow window)
        {
            var bindings = new List<CommandBindingData>();

            bindings.Add(new CommandBindingData(AppCommand.Exit,
                                                p => ExitApplication()));

            bindings.Add(new CommandBindingData(AppCommand.About,
                                                p => ShowAboutDialog()));

            bindings.Add(new CommandBindingData(AppCommand.ShowUpdatesDialogCommand,
                                               p => ShowUpdatesDialog()));


            bindings.Add(new CommandBindingData(AppCommand.ProgramSettings,
                                                p => ShowSettingsDialog()));

            bindings.Add(new CommandBindingData(AppCommand.ShowToolWindow,
                                                p =>
                                                {
                                                    var toolwindowviewmodel = p as IToolWindow;
                                                    ShowToolWindow(toolwindowviewmodel);
                                                }));

            bindings.Add(new CommandBindingData(AppCommand.New,
                                             async p =>
                                               {
                                                   await CreateNewSolution();
                                               }));

            bindings.Add(new CommandBindingData(AppCommand.Open,
                                             async p =>
                                             {
                                                 await OpenSolutionFromDialog();
                                             }));

            bindings.Add(new CommandBindingData(AppCommand.CloseFile,
                                             p =>
                                            {
                                                CloseCurrentDocument();
                                            },
                                             p => CanCloseCurrentDocument()));

            //Close Solution
            bindings.Add(new CommandBindingData(AppCommand.CloseSolution,
                                             p =>
                                             {
                                                 CloseSolution();
                                             },
                                             p => CanCloseSolution()));

            bindings.Add(new CommandBindingData(AppCommand.ShowStartPage,
                                            p =>
                                            {
                                                ShowStartPage();
                                            }));

            //open solution (from Recent list)
            bindings.Add(new CommandBindingData(AppCommand.LoadFile,
                                             async p =>
                                             {
                                                 await OpenSolution(p as string);
                                             }));

            //saves current active document
            bindings.Add(new CommandBindingData(AppCommand.Save,
                                            p =>
                                            {
                                                SaveCurrentDocument();
                                            },
                                            p => CanSaveCurrentDocument()));


            bindings.Add(new CommandBindingData(AppCommand.SaveAll,
                                           p =>
                                           {
                                               SaveAll();
                                           }));

            bindings.Add(new CommandBindingData(AppCommand.BuildCommand,
                                         async p =>
                                           {
                                               await RunBuild(p as SolutionExplorerNodeModel);
                                           },
                                         p => p is SolutionProjectNodeModel || p is SolutionRootNodeModel));

            bindings.Add(new CommandBindingData(AppCommand.CompileCommand,
                                       async p =>
                                       {
                                           await RunCompile(p as SolutionExplorerNodeModel);
                                       },
                                       p => p is SolutionProjectNodeModel || p is SolutionRootNodeModel));

            bindings.Add(new CommandBindingData(AppCommand.PackCommand,
                                      async p =>
                                      {
                                          await RunPack(p as SolutionExplorerNodeModel);
                                      },
                                      p => p is SolutionProjectNodeModel || p is SolutionRootNodeModel));

            bindings.Add(new CommandBindingData(AppCommand.RestorePackagesCommand,
                          async p =>
                          {
                              await RestorePackages(p as SolutionExplorerNodeModel);
                          },
                          p => p is SolutionProjectNodeModel || p is SolutionRootNodeModel));

            #region Add reference to project

            bindings.Add(new CommandBindingData(AppCommand.ManageReferencesCommand,
                                        p =>
                                       {
                                           try
                                           {
                                               ShowProjectReferences(p as SolutionExplorerNodeModel);
                                           }
                                           catch (Exception ex)
                                           {
                                               MessageDialog.Show(ex.Message);
                                           }
                                       },
                                       p => p is SolutionProjectNodeModel || p is ProjectReferencesNodeModel));

            bindings.Add(new CommandBindingData(AppCommand.ShowPackageManagerCommand,
                                       p =>
                                       {
                                           ShowPackageManager(p as SolutionExplorerNodeModel);
                                       },
                                      p => p is SolutionProjectNodeModel || p is ProjectReferencesNodeModel));

            #endregion


            #region Add Project Command

            bindings.Add(new CommandBindingData(AppCommand.AddProjectCommand,
                                        p =>
                                       {
                                           AddNewProject(p as SolutionExplorerNodeModel);
                                       },
                                       p => p is SolutionVirtualFolderNodeModel || p is SolutionRootNodeModel));


            #endregion Add Project Command

            #region Add Folder Command

            bindings.Add(new CommandBindingData(AppCommand.AddFolderCommand,
                                         async p =>
                                         {
                                             await AddNewFolder(p as FilesContainerNodeModel);
                                         },
                                         p => p is FilesContainerNodeModel));

            #endregion Add Folder Command

            #region Add Symbol Command

            bindings.Add(new CommandBindingData(AppCommand.AddNewSymbolCommand,
                                         async p =>
                                         {
                                             await AddNewSymbolItem(p as FilesContainerNodeModel);
                                         },
                                         p => p is FilesContainerNodeModel));

            #endregion Add Symbol Command

            #region Add Footprint Command

            bindings.Add(new CommandBindingData(AppCommand.AddNewFootprintCommand,
                                          async p =>
                                          {
                                              await AddNewFootprintItem(p as FilesContainerNodeModel);
                                          },
                                          p => p is FilesContainerNodeModel));

            #endregion Add Footprint Command

            #region Add Model Command

            bindings.Add(new CommandBindingData(AppCommand.AddNewModelCommand,
                                          async p =>
                                          {
                                              await AddNewModelItem(p as FilesContainerNodeModel);
                                          },
                                          p => p is FilesContainerNodeModel));

            #endregion Add Model Command

            #region Add Component Command

            bindings.Add(new CommandBindingData(AppCommand.AddNewComponentCommand,
                                           async p =>
                                           {
                                               await AddNewComponentItem(p as FilesContainerNodeModel);
                                           },
                                           p => p is FilesContainerNodeModel));

            #endregion Add component

            #region Add Schematic Command

            bindings.Add(new CommandBindingData(AppCommand.AddNewSchematicCommand,
                                           async p =>
                                           {
                                               await AddNewSchematicItem(p as FilesContainerNodeModel);
                                           },
                                           p =>
                                           {
                                               var container = p as FilesContainerNodeModel;
                                               return container != null && container.ProjectNode.ProjectOutputType == ProjectOutputType.Board;
                                           }));

            #endregion Add Schematic Command

            #region Add Board Command

            bindings.Add(new CommandBindingData(AppCommand.AddNewBoardCommand,
                                           async p =>
                                           {
                                               await AddNewBoardItem(p as FilesContainerNodeModel);
                                           },
                                           p =>
                                           {
                                               var container = p as FilesContainerNodeModel;
                                               return container != null && container.ProjectNode.ProjectOutputType == ProjectOutputType.Board;
                                           }));

            #endregion Add Board Command

            #region Add New Item Command

            bindings.Add(new CommandBindingData(AppCommand.AddNewItemCommand,
                                           async p =>
                                           {
                                               await AddNewGenericItem(p as FilesContainerNodeModel);
                                           },
                                           p => p is FilesContainerNodeModel));

            #endregion

            #region Add Existing Item Command

            bindings.Add(new CommandBindingData(AppCommand.AddExistingItemCommand,
                                           async p =>
                                           {
                                               await AddExistingItems(p as FilesContainerNodeModel);
                                           },
                                           p => p is FilesContainerNodeModel));

            #endregion

            #region Open Item Command

            bindings.Add(new CommandBindingData(AppCommand.OpenItemCommand,
                                           async p =>
                                           {
                                               var item = p as SolutionExplorerNodeModel;
                                               var filePath = item.GetItemFullPath();
                                               await Open(item, filePath);
                                           },
                                           p => p is ProjectSymbolNodeModel
                                             || p is ProjectSchematicNodeModel
                                             || p is ProjectFootprintNodeModel
                                             || p is ProjectModelNodeModel
                                             || p is ProjectComponentNodeModel
                                             || p is ProjectBoardNodeModel
                                             || p is ProjectFontNodeModel
                                             //...
                                             || p is ProjectGenericFileNodeModel));

            #endregion Open Item Command

            bindings.Add(new CommandBindingData(AppCommand.ShowPropertiesCommand,
                                           async p =>
                                           {
                                               await ShowProperties(p as SolutionExplorerNodeModel);
                                           },
                                           p => p is SolutionProjectNodeModel || p is SolutionRootNodeModel));


            bindings.Add(new CommandBindingData(AppCommand.ImportEagleCommand,
               p => ImportFromEagle(),
               p => true));

            bindings.Add(new CommandBindingData(AppCommand.ChangeModeCommand,
                                          p =>
                                          {
                                              ChangeMode();
                                          }));


            bindings.Add(new CommandBindingData(AppCommand.CyclePlacementOrRotateCommand,
                                          p =>
                                          {
                                              CyclePlacementOrRotate();
                                          },
                                          canExecute: p => activeDocument is ICanvasDesignerFileViewModel,
                                          handledAction: p => true));

            bindings.Add(new CommandBindingData(AppCommand.DeleteSelectedItemsCommand,
                                   p =>
                                   {
                                       DeleteSelectedItems();
                                   },
                                   canExecute: p => activeDocument is ICanvasDesignerFileViewModel,
                                   handledAction: p => true));

            bindings.Add(new CommandBindingData(AppCommand.CopySelectedItemsCommand,
                                   p =>
                                   {
                                       CopySelectedItems();
                                   },
                                   canExecute: p => activeDocument is ICanvasDesignerFileViewModel,
                                   handledAction: p => true));

            bindings.Add(new CommandBindingData(AppCommand.PasteSelectedItemsCommand,
                                   p =>
                                   {
                                       PasteSelectedItems();
                                   },
                                   canExecute: p => activeDocument is ICanvasDesignerFileViewModel,
                                   handledAction: p => true));

            bindings.Add(new CommandBindingData(AppCommand.UndoCommand,
                                   p =>
                                   {
                                       Undo();
                                   },
                                   canExecute: p => activeDocument is ICanvasDesignerFileViewModel,
                                   handledAction: p => true));

            bindings.Add(new CommandBindingData(AppCommand.RedoCommand,
                                  p =>
                                  {
                                      Redo();
                                  },
                                  canExecute: p => activeDocument is ICanvasDesignerFileViewModel,
                                  handledAction: p => true));

            window.AddCommanBindings(bindings);
        }

        ICommand closeAllFilesCommand;
        public ICommand CloseAllFilesCommand
        {
            get
            {
                if (closeAllFilesCommand == null)
                    closeAllFilesCommand = CreateCommand(p => CloseAllFiles(),
                                                         p => CanCloseAllFiles());

                return closeAllFilesCommand;
            }
        }

        ICommand closeAllFilesExceptCurrentCommand;
        public ICommand CloseAllFilesExceptCurrentCommand
        {
            get
            {
                if (closeAllFilesExceptCurrentCommand == null)
                    closeAllFilesExceptCurrentCommand = CreateCommand(p => CloseAllFilesExceptCurrent(),
                                                                      p => CanCloseAllFilesExceptCurrent());

                return closeAllFilesExceptCurrentCommand;
            }
        }

    }
}

﻿using IDE.Core;
using IDE.Core.Commands;
using IDE.Core.Settings;
using IDE.Core.Storage;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using IDE.Core.Interfaces;
using System.Collections.ObjectModel;

namespace IDE.Documents.Views
{
    public class AddReferencesDialogViewModel : DialogViewModel
    {

        public AddReferencesDialogViewModel(ISolutionProjectNodeModel project)
        {
            _project = project;
            Load(project);
        }

        private readonly ISolutionProjectNodeModel _project;

        public string WindowTitle
        {
            get
            {
                return "Add Reference";
            }
        }

        int selectedTabIndex;

        public int SelectedTabIndex
        {
            get { return selectedTabIndex; }
            set
            {
                selectedTabIndex = value;
                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }

        public SortableObservableCollection<ProjectDocumentReference> AvailableLibraries { get; set; } = new SortableObservableCollection<ProjectDocumentReference>();

        public SortableObservableCollection<ProjectDocumentReference> AvailableProjects { get; set; } = new SortableObservableCollection<ProjectDocumentReference>();

        public SortableObservableCollection<IProjectDocumentReference> ReferencesList { get; set; } = new SortableObservableCollection<IProjectDocumentReference>();

        List<ProjectDocumentReference> allLibraries = new List<ProjectDocumentReference>();

        List<ProjectDocumentReference> allProjects = new List<ProjectDocumentReference>();

        private void AddReference(IList selectedLibs, Collection<ProjectDocumentReference> availableCollection)
        {
            if (selectedLibs == null)
                return;

            var toremove = new List<ProjectDocumentReference>();
            foreach (ProjectDocumentReference lib in selectedLibs)
            {
                toremove.Add(lib);
                ReferencesList.Add(lib);
            }

            if (availableCollection != null)
                toremove.ForEach(l => availableCollection.Remove(l));

            ReferencesList.SortAscending(pr => pr.ToString());
        }

        ICommand addLibraryCommand;

        public ICommand AddLibraryCommand
        {
            get
            {
                if (addLibraryCommand == null)
                    addLibraryCommand = CreateCommand(p =>
                    {
                        var selectedLibs = p as IList;

                        AddReference(selectedLibs, AvailableLibraries);
                    }
                   );
                return addLibraryCommand;
            }
        }

        ICommand addProjectCommand;
        public ICommand AddProjectCommand
        {
            get
            {
                if (addProjectCommand == null)
                    addProjectCommand = CreateCommand(p =>
                    {
                        var selectedLibs = p as IList;

                        AddReference(selectedLibs, AvailableProjects);
                    }
                   );
                return addProjectCommand;
            }
        }

        ICommand browseLibrariesCommand;
        public ICommand BrowseLibrariesCommand
        {
            get
            {
                if (browseLibrariesCommand == null)
                    browseLibrariesCommand = CreateCommand(p =>
                    {
                        var dlg = ServiceProvider.Resolve<IOpenFileDialog>();
                        dlg.Multiselect = true;
                        dlg.Filter = "Libraries (*.library)|*.library";

                        if (dlg.ShowDialog() == true)
                        {
                            var projectFolder = _project.GetItemFolderFullPath();

                            var libsToAdd = new List<ProjectDocumentReference>();
                            foreach (var libFile in dlg.FileNames)
                            {
                                var libDoc = XmlHelper.Load<LibraryDocument>(libFile);

                                var relativePath = DirectoryName.GetRelativePath(projectFolder, libFile);
                                var actualPath = PathName.NormalizePath(relativePath);

                                libsToAdd.Add(new LibraryProjectReference
                                {
                                    HintPath = relativePath,
                                    LibraryName = Path.GetFileNameWithoutExtension(libFile),
                                    Version = libDoc.Version
                                });
                            }

                            AddReference(libsToAdd, null);
                        }
                    }
                   );
                return browseLibrariesCommand;
            }
        }

        ICommand removeReferencesCommand;

        public ICommand RemoveReferencesCommand
        {
            get
            {
                if (removeReferencesCommand == null)
                    removeReferencesCommand = CreateCommand(p =>
                    {
                        var selectedLibs = p as IList;
                        if (selectedLibs == null)
                            return;

                        var toremove = selectedLibs.Cast<ProjectDocumentReference>().ToList();// new List<ProjectDocumentReference>();

                        toremove.ForEach(l =>
                        {
                            if (l is LibraryProjectReference)
                                AvailableLibraries.Add(l);
                            else if (l is ProjectProjectReference)
                                AvailableProjects.Add(l);


                            ReferencesList.Remove(l);

                        });

                        AvailableLibraries.SortAscending(pr => pr.ToString());
                        AvailableProjects.SortAscending(pr => pr.ToString());
                        ReferencesList.SortAscending(pr => pr.ToString());
                    }//,
                     // p => currentClassNode as NetClassDesignerItem != null);
                   );
                return removeReferencesCommand;
            }
        }

        void Load(ISolutionProjectNodeModel project)
        {
            allLibraries.Clear();
            allProjects.Clear();
            ReferencesList.Clear();
            AvailableLibraries.Clear();
            AvailableProjects.Clear();

            //var thisProjectName = Path.GetFileNameWithoutExtension(project.FileName);
            var thisProjectName = project.Name;

            var projReferences = project.Project.References;
            var projFolder = project.GetItemFolderFullPath();
            //load version of reference
            foreach (var pr in projReferences.OfType<LibraryProjectReference>())
            {
                var refFile = Path.Combine(projFolder, "References", $"{pr.LibraryName}.libref");
                if (File.Exists(refFile))
                {
                    var libDoc = XmlHelper.Load<LibraryDocument>(refFile);
                    pr.Version = libDoc.Version;
                }
            }

            ReferencesList.AddRange(projReferences);

            //libraries from known folders (from settings); for now, hard-coded

            allLibraries.Clear();
            var s = ServiceProvider.Resolve<ISettingsManager>()//ServiceProvider.GetService<SettingsManager>()
                        .GetSetting<EnvironmentFolderLibsSettingData>();
            if (s != null)
            {
                foreach (var libFolder in s.Folders)
                {
                    foreach (var libFile in Directory.GetFiles(libFolder, "*.library", SearchOption.AllDirectories))
                    {
                        var libDoc = XmlHelper.Load<LibraryDocument>(libFile);

                        allLibraries.Add(new LibraryProjectReference
                        {
                            HintPath = libFile,
                            LibraryName = Path.GetFileNameWithoutExtension(libFile),
                            Version = libDoc.Version
                        });
                    }
                }
            }

            //except current references
            AvailableLibraries.AddRange(allLibraries.Cast<LibraryProjectReference>().Where(l => !ReferencesList.Any(r => l.IsSame(r as LibraryProjectReference))));

            //library projects from current solution except current project
            var solutionDoc = SolutionManager.Solution;
            foreach (var solProj in solutionDoc.Children.OfType<SolutionProjectItem>())
            {
                var pName = Path.GetFileNameWithoutExtension(solProj.RelativePath);
                if (pName != thisProjectName)
                {
                    allProjects.Add(new ProjectProjectReference { ProjectPath = solProj.RelativePath });
                }
            }

            AvailableProjects.AddRange(allProjects.Where(l => !ReferencesList.Any(r => l.ToString() == r.ToString())));

            AvailableLibraries.SortAscending(p => p.ToString());
            AvailableProjects.SortAscending(p => p.ToString());
            ReferencesList.SortAscending(p => p.ToString());
        }

    }

    enum TabReferenceType
    {
        Libraries,
        Projects,
        Browse
    }
}
